using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Promatis.Net.Data;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public abstract class ReferenceTreeService<T>(IDbContextFactory<ApplicationDbContext> contextFactory)
    : ReferenceService<T>(contextFactory), IReferenceTreeService<T>
    where T : ReferenceTreeBase<T>
{
    public async Task<List<T>> GetRootsAsync()
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking() // Ускоряем чтение
            .Where(x => x.ParentId == null)
            .ToListAsync();
    }

    public async Task<List<T>> GetChildrenAsync(Guid parentId)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => x.ParentId == parentId)
            .ToListAsync();
    }

    public async Task<List<T>> GetParentPathAsync(Guid id)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        var path = new List<T>();

        // Используем один контекст на весь цикл
        T? current = await context.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        while (current != null)
        {
            path.Insert(0, current);

            if (current.ParentId == null)
                break;

            Guid parentId = current.ParentId.Value;

            // Заменяем FindAsync на FirstOrDefaultAsync для надежной работы QueryFilter
            current = await context.Set<T>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentId);

            if (current == null) break;
        }

        return path;
    }

    public async Task<T?> GetFullTreeAsync(Guid rootId)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();

        // 1. Загружаем все записи, которые потенциально могут быть в этом дереве.
        // Для TPT (UnitBase) EF Core сам сделает необходимые Join-ы.
        // Глобальный фильтр SoftDelete отсечет удаленные узлы автоматически.
        List<T> allItems = await context.Set<T>()
            .AsNoTracking()
            .ToListAsync();

        // 2. Строим словарь для быстрого поиска
        ILookup<Guid?, T> lookup = allItems.ToLookup(x => x.ParentId);

        // 3. Находим корневой элемент
        T? root = allItems.FirstOrDefault(x => x.Id == rootId);
        if (root == null) return null;

        // 4. Рекурсивно связываем объекты в памяти
        BuildTree(root, lookup);

        return root;
    }

    private void BuildTree(T parent, ILookup<Guid?, T> lookup)
    {
        // Находим всех детей для текущего родителя в локальном списке
        List<T> children = lookup[parent.Id].ToList();
        parent.Children = children;

        foreach (T child in children)
        {
            child.Parent = parent; // Устанавливаем обратную связь
            BuildTree(child, lookup); // Рекурсия по локальным данным (без запросов к БД)
        }
    }

    public virtual async Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync(ct);

        // Загружаем сущность С ТРЕКИНГОМ (без AsNoTracking), чтобы сохранить изменения
        T entity = await context.Set<T>()
                       .FirstOrDefaultAsync(x => x.Id == id, ct)
                   ?? throw new Exception($"Entity {typeof(T).Name} with id {id} not found");

        if (entity.ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            if (newParentId == id)
                throw new InvalidOperationException("Узел не может быть родителем самого себя.");

            // Проверка цикла через существующий метод
            List<T> path = await GetParentPathAsync(newParentId.Value);
            if (path.Any(x => x.Id == id))
                throw new InvalidOperationException("Циклическая зависимость: нельзя переместить узел в своё поддерево.");

            // Здесь можно добавить проверку: существует ли вообще новый родитель в базе
            bool parentExists = await context.Set<T>().AnyAsync(x => x.Id == newParentId.Value, ct);
            if (!parentExists)
                throw new Exception("Новый родитель не найден.");
        }

        entity.ParentId = newParentId;

        // SaveChangesAsync инициирует IBeforeSaveTrigger (UnitBaseHierarchyTrigger)
        await context.SaveChangesAsync(ct);
    }
}