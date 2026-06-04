using Promatis.Net.Data;

namespace Promatis.Net.UI;

public class HierarchicalOzuMutationStrategy<TEntity> : IOzuMutationStrategy<TEntity> where TEntity : class
{
    private readonly Func<TEntity, object?> _parentIdSelector;

    // Работаем строго с живой ICollection навигационного свойства, без клонирования
    private readonly Func<TEntity, ICollection<TEntity>> _childrenSelector;

    // Быстрый индекс для мгновенного поиска за O(1)
    private readonly Dictionary<object, TEntity> _nodeIndex = [];
    private readonly Dictionary<object, object> _childToParentMap = []; // Карта связей: IdУзла -> IdРодителя

    private bool _isIndexBuilt;

    public HierarchicalOzuMutationStrategy(
        Func<TEntity, object?> parentIdSelector,
        Func<TEntity, ICollection<TEntity>> childrenSelector)
    {
        _parentIdSelector = parentIdSelector ?? throw new ArgumentNullException(nameof(parentIdSelector));
        _childrenSelector = childrenSelector ?? throw new ArgumentNullException(nameof(childrenSelector));
    }

    public void ApplyDelta(ICollection<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector)
    {
        object? entityId = idSelector(entity);
        if (entityId == null) return;

        // Ленивая сборка плоского индекса при первой мутации данных формы
        if (!_isIndexBuilt)
        {
            BuildIndexRecursive(inMemoryItems, idSelector);
            _isIndexBuilt = true;
        }

        switch (state)
        {
            case EntityStateChangeEnum.Added:
                HandleAdded(inMemoryItems, entity, entityId, idSelector);
                break;

            case EntityStateChangeEnum.Modified:
                HandleModified(inMemoryItems, entity, entityId, idSelector);
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                HandleDeleted(inMemoryItems, entityId, idSelector);
                break;
        }
    }

    private void HandleAdded(ICollection<TEntity> rootItems, TEntity entity, object entityId, Func<TEntity, object?> idSelector)
    {
        if (_nodeIndex.ContainsKey(entityId)) return; // Защита от дублирования в памяти

        object? newParentId = _parentIdSelector(entity);

        if (newParentId == null)
        {
            // Объект корневой — добавляем в корень ОЗУ
            rootItems.Add(entity);
        }
        else if (_nodeIndex.TryGetValue(newParentId, out var parentNode))
        {
            // Объект дочерний — добавляем в живую коллекцию его родителя
            var children = _childrenSelector(parentNode);
            children?.Add(entity);
            _childToParentMap[entityId] = newParentId;
        }

        // Каскадно регистрируем узел и его поддерево в быстром индексе O(1)
        UpdateIndexForSubtree(entity, idSelector);
    }

    private void HandleModified(ICollection<TEntity> rootItems, TEntity entity, object entityId, Func<TEntity, object?> idSelector)
    {
        // ИСПРАВЛЕНО: Никакого маппинга. Если узел уже существовал, хирургически удаляем старую ссылку
        if (_nodeIndex.TryGetValue(entityId, out var oldNodeReference))
        {
            object? oldParentId = _childToParentMap.TryGetValue(entityId, out var pId) ? pId : null;

            if (oldParentId == null)
            {
                rootItems.Remove(oldNodeReference);
            }
            else if (_nodeIndex.TryGetValue(oldParentId, out var oldParentNode))
            {
                _childrenSelector(oldParentNode)?.Remove(oldNodeReference);
            }

            // Вычищаем старую ссылку и её связи из индекса перед добавлением новой
            RemoveFromIndexRecursive(oldNodeReference, idSelector);
        }

        // Вставляем новую ссылку по актуальному ParentId (идеально обрабатывает и Re-parenting, и изменение полей)
        HandleAdded(rootItems, entity, entityId, idSelector);
    }

    private void HandleDeleted(ICollection<TEntity> rootItems, object entityId, Func<TEntity, object?> idSelector)
    {
        if (!_nodeIndex.TryGetValue(entityId, out var nodeToRemove)) return;

        object? parentId = _childToParentMap.TryGetValue(entityId, out var pId) ? pId : null;

        if (parentId == null)
        {
            rootItems.Remove(nodeToRemove);
        }
        else if (_nodeIndex.TryGetValue(parentId, out var parentNode))
        {
            _childrenSelector(parentNode)?.Remove(nodeToRemove);
        }

        // Каскадно вычищаем удаленный подграф из индекса
        RemoveFromIndexRecursive(nodeToRemove, idSelector);
    }

    // --- Служебные методы управления быстрым индексом O(1) ---

    private void BuildIndexRecursive(ICollection<TEntity> nodes, Func<TEntity, object?> idSelector, object? parentId = null)
    {
        foreach (var node in nodes)
        {
            object? id = idSelector(node);
            if (id == null) continue;

            _nodeIndex[id] = node;
            if (parentId != null) _childToParentMap[id] = parentId;

            var children = _childrenSelector(node);
            if (children != null && children.Count > 0)
            {
                BuildIndexRecursive(children, idSelector, id);
            }
        }
    }

    private void UpdateIndexForSubtree(TEntity node, Func<TEntity, object?> idSelector)
    {
        object? id = idSelector(node);
        if (id == null) return;

        _nodeIndex[id] = node;

        object? parentId = _parentIdSelector(node);
        if (parentId != null) _childToParentMap[id] = parentId;

        var children = _childrenSelector(node);
        if (children != null)
        {
            foreach (var child in children)
            {
                UpdateIndexForSubtree(child, idSelector);
            }
        }
    }

    private void RemoveFromIndexRecursive(TEntity node, Func<TEntity, object?> idSelector)
    {
        object? id = idSelector(node);
        if (id == null) return;

        _nodeIndex.Remove(id);
        _childToParentMap.Remove(id);

        var children = _childrenSelector(node);
        if (children != null)
        {
            foreach (var child in children)
            {
                RemoveFromIndexRecursive(child, idSelector);
            }
        }
    }
}