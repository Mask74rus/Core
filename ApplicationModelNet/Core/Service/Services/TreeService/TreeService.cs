using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.Service;

/// <summary>
/// Универсальный базовый класс реализации бизнес-логики для ЛЮБЫХ древовидных структур в системе.
/// Инкапсулирует тяжелые иерархические алгоритмы СУБД, проверку циклов циклической зависимости и ОЗУ-сборку графов.
/// </summary>
/// <typeparam name="T">Тип доменного объекта, реализующего ITreeNode.</typeparam>
/// <typeparam name="TContext">Контекст базы данных DbContext.</typeparam>
public abstract class TreeService<T, TContext>(IDbContextFactory<TContext> contextFactory) 
    : BaseService<T, Guid, TContext>(contextFactory), ITreeService<T>
    where T : class, ITreeNode<T>, IDomainObjectHasKey<Guid>
    where TContext : DbContext
{
    public async Task<List<T>> GetRootsAsync()
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().AsNoTracking().Where(x => x.ParentId == null).ToListAsync();
    }

    public async Task<List<T>> GetChildrenAsync(Guid parentId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().AsNoTracking().Where(x => x.ParentId == parentId).ToListAsync();
    }

    public async Task<List<T>> GetParentPathAsync(Guid id)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        var path = new List<T>();
        T? current = await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        while (current != null)
        {
            path.Insert(0, current);
            if (current.ParentId == null) break;
            Guid parentId = current.ParentId.Value;
            current = await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == parentId);
            if (current == null) break;
        }
        return path;
    }

    public async Task<T?> GetFullTreeAsync(Guid rootId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        List<T> allItems = await context.Set<T>().AsNoTracking().ToListAsync();
        ILookup<Guid?, T> lookup = allItems.ToLookup(x => x.ParentId);
        T? root = allItems.FirstOrDefault(x => x.Id == rootId);
        if (root == null) return null;

        BuildTree(root, lookup);
        return root;
    }

    private void BuildTree(T parent, ILookup<Guid?, T> lookup)
    {
        List<T> children = lookup[parent.Id].ToList();
        parent.Children.Clear();
        foreach (T child in children)
        {
            parent.Children.Add(child);
            PropertyInfo? parentProp = child.GetType().GetProperty(nameof(ITreeNode<T>.Parent));
            if (parentProp is { CanWrite: true }) parentProp.SetValue(child, parent);
            BuildTree(child, lookup);
        }
    }

    public virtual async Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync(ct);
        T entity = await context.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct)
                   ?? throw new Exception($"Сущность {typeof(T).Name} с ID {id} не найдена.");

        if (entity.ParentId == newParentId) return;

        if (newParentId.HasValue)
        {
            if (newParentId == id) throw new InvalidOperationException("Узел не может быть родителем самому себе.");
            List<T> path = await GetParentPathAsync(newParentId.Value);
            if (path.Any(x => x.Id == id)) throw new InvalidOperationException("Циклическая зависимость.");
            bool parentExists = await context.Set<T>().AnyAsync(x => x.Id == newParentId.Value, ct);
            if (!parentExists) throw new Exception("Новый родитель не найден.");
        }

        entity.ParentId = newParentId;
        await context.SaveChangesAsync(ct);
    }

    public abstract Task<T> CreateChildTemplateAsync(T parent);
}