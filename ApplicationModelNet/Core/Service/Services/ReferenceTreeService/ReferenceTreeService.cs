using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Service;

/// <summary>
/// Универсальный базовый сервис для работы со всеми древовидными структурами в системе.
/// Наследует логику обычных справочников (ReferenceService) и расширяет её для иерархий.
/// </summary>
/// <typeparam name="T">Тип сущности, унаследованный от ReferenceTreeBase.</typeparam>
/// <typeparam name="TContext">Контекст базы данных.</typeparam>
public abstract class ReferenceTreeService<T, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceService<T, TContext>(contextFactory), IReferenceTreeService<T>
    where T : ReferenceTreeBase, ITreeNode<T>
    where TContext : DbContext
{
    public async Task<List<T>> GetRootsAsync()
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => x.ParentId == null)
            .ToListAsync();
    }

    public async Task<List<T>> GetChildrenAsync(Guid parentId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .ToListAsync();
    }

    public async Task<List<T>> GetParentPathAsync(Guid id)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        var path = new List<T>();

        T? current = await context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        while (current != null)
        {
            path.Insert(0, current);

            if (current.ParentId == null)
                break;

            Guid parentId = current.ParentId.Value;

            current = await context.Set<T>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentId);

            if (current == null) break;
        }

        return path;
    }

    public async Task<T?> GetFullTreeAsync(Guid rootId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // 1. Выкачиваем все записи справочника одним быстрым запросом.
        // Глобальный фильтр SoftDelete отсечет удаленные узлы автоматически.
        List<T> allItems = await context.Set<T>()
            .AsNoTracking()
            .ToListAsync();

        // 2. Строим индекс (Lookup) по физическому полю ParentId
        ILookup<Guid?, T> lookup = allItems.ToLookup(x => x.ParentId);

        // 3. Находим корневой элемент
        T? root = allItems.FirstOrDefault(x => x.Id == rootId);
        if (root == null) return null;

        // 4. Рекурсивно связываем объекты в памяти через стандартные навигационные свойства
        BuildTree(root, lookup);

        return root;
    }

    // Метод BuildTree теперь пишется БЕЗ ключевого слова where
    private void BuildTree(T parent, ILookup<Guid?, T> lookup)
    {
        // 1. Получаем список дочерних элементов для текущего родителя
        // Свойство Id доступно из вашей базовой структуры ReferenceBase / ReferenceTreeBase
        List<T> children = lookup[parent.Id].ToList();

        // 2. Очищаем коллекцию (работает напрямую, так как T ограничен интерфейсом ITreeNode<T>)
        parent.Children.Clear();

        // 3. Заполняем дерево и уходим в рекурсию
        foreach (T child in children)
        {
            parent.Children.Add(child);

            // Так как у свойства Parent в интерфейсе нет сеттера (get-only),
            // мы безопасно прописываем его через проверку реального свойства C#-класса
            PropertyInfo? parentProp = child.GetType().GetProperty(nameof(ITreeNode<T>.Parent));
            if (parentProp is { CanWrite: true })
            {
                parentProp.SetValue(child, parent);
            }

            // Рекурсивно собираем поддерево для потомка
            BuildTree(child, lookup);
        }
    }

    public virtual async Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync(ct);

        // Загружаем сущность С ТРЕКИНГОМ (без AsNoTracking), чтобы сохранить изменения
        T entity = await context.Set<T>()
                       .FirstOrDefaultAsync(x => x.Id == id, ct)
                   ?? throw new Exception($"Entity {typeof(T).Name} with id {id} not found");

        if (entity.ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            if (newParentId == id)
                throw new InvalidOperationException("Узел не может быть родителем самому себе.");

            // Эффективная проверка цикла на базе пути родителя
            List<T> path = await GetParentPathAsync(newParentId.Value);
            if (path.Any(x => x.Id == id))
                throw new InvalidOperationException("Циклическая зависимость: нельзя переместить узел в своё поддерево.");

            bool parentExists = await context.Set<T>().AnyAsync(x => x.Id == newParentId.Value, ct);
            if (!parentExists)
                throw new Exception("Новый родитель не найден.");
        }

        entity.ParentId = newParentId;

        // Сохранение инициирует IBeforeSaveTrigger (UnitBaseHierarchyTrigger / ReferenceTreeParentTrigger)
        await context.SaveChangesAsync(ct);
    }

    public override async Task UpdateAsync(T entity)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // 1. Загружаем чистую, актуальную запись С ТРЕКИНГОМ напрямую из базы данных по Id
        T? dbEntity = await context.Set<T>().FirstOrDefaultAsync(x => x.Id == entity.Id);

        if (dbEntity == null)
            throw new Exception($"Сущность {typeof(T).Name} с ID {entity.Id} не найдена для обновления.");

        // 2. Накатываем новые значения, пришедшие с UI-карточки, на объект из базы.
        // Это заставит EF Core сравнить старые данные из БД и новые с UI, 
        // выставит IsModified = true ТОЛЬКО для реально изменившихся полей 
        // и сохранит настоящие OriginalValues в ChangeTracker!
        context.Entry(dbEntity).CurrentValues.SetValues(entity);

        // 3. Сохраняем изменения. Теперь ваш оригинальный интерцептор (CaptureChanges) 
        // идеально поймает разницу между старым и новым, а AuditLog запишет честную дельту!
        await context.SaveChangesAsync();
    }
}
