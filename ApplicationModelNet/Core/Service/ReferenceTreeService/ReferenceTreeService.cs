using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

/// <summary>
/// Универсальный базовый сервис для работы со всеми древовидными структурами в системе.
/// Наследует логику обычных справочников (ReferenceService) и расширяет её для иерархий.
/// </summary>
/// <typeparam name="T">Тип сущности, унаследованный от ReferenceTreeBase.</typeparam>
/// <typeparam name="TContext">Контекст базы данных.</typeparam>
public abstract class ReferenceTreeService<T, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceService<T, TContext>(contextFactory), IReferenceTreeService<T>
    where T : ReferenceTreeBase
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

    private void BuildTree(T parent, ILookup<Guid?, T> lookup)
    {
        List<T> children = lookup[parent.Id].ToList();

        // Прямо очищаем и наполняем стандартную коллекцию Children.
        // Благодаря отсутствию каскада дженериков нам больше не нужны обертки
        parent.Children.Clear();

        foreach (T child in children)
        {
            parent.Children.Add(child);
            child.Parent = parent; // Восстанавливаем прямую ссылку на родителя в графе объектов

            // Продолжаем рекурсивную сборку для текущего потомка
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
}
