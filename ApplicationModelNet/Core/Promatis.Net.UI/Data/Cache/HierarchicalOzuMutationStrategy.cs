using Promatis.Net.Data;

namespace Promatis.Net.UI;

public class HierarchicalOzuMutationStrategy<TEntity> : IOzuMutationStrategy<TEntity> where TEntity : class
{
    private readonly Func<TEntity, object?> _parentIdSelector;
    private readonly Func<TEntity, List<TEntity>> _childrenSelector;

    public HierarchicalOzuMutationStrategy(Func<TEntity, object?> parentIdSelector, Func<TEntity, List<TEntity>> childrenSelector)
    {
        _parentIdSelector = parentIdSelector ?? throw new ArgumentNullException(nameof(parentIdSelector));
        _childrenSelector = childrenSelector ?? throw new ArgumentNullException(nameof(childrenSelector));
    }

    public void ApplyDelta(List<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector)
    {
        object? entityId = idSelector(entity);
        if (entityId == null) return;

        // Если объект корневой (ParentId == null), обрабатываем его на верхнем уровне списка
        object? parentId = _parentIdSelector(entity);
        if (parentId == null)
        {
            ProcessNodeInList(inMemoryItems, state, entity, entityId, idSelector);
            return;
        }

        // Если у объекта есть родитель, ищем этого родителя по всему дереву рекурсивно
        TEntity? parentNode = FindNodeRecursive(inMemoryItems, parentId, idSelector);
        if (parentNode != null)
        {
            List<TEntity> siblings = _childrenSelector(parentNode) ?? new List<TEntity>();
            ProcessNodeInList(siblings, state, entity, entityId, idSelector);
        }
    }

    private void ProcessNodeInList(List<TEntity> list, EntityStateChangeEnum state, TEntity entity, object entityId, Func<TEntity, object?> idSelector)
    {
        switch (state)
        {
            case EntityStateChangeEnum.Added:
                if (!list.Any(x => Equals(idSelector(x), entityId))) list.Add(entity);
                break;

            case EntityStateChangeEnum.Modified:
                int idx = list.FindIndex(x => Equals(idSelector(x), entityId));
                if (idx >= 0) list[idx] = entity;
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                TEntity? toRemove = list.FirstOrDefault(x => Equals(idSelector(x), entityId));
                if (toRemove != null) list.Remove(toRemove);
                break;
        }
    }

    private TEntity? FindNodeRecursive(List<TEntity> nodes, object targetId, Func<TEntity, object?> idSelector)
    {
        foreach (var node in nodes)
        {
            if (Equals(idSelector(node), targetId)) return node;

            List<TEntity> children = _childrenSelector(node);
            if (children != null && children.Any())
            {
                TEntity? found = FindNodeRecursive(children, targetId, idSelector);
                if (found != null) return found;
            }
        }
        return null;
    }
}