using System.Reflection;
using MudBlazor;
using Promatis.Net.Data;

namespace Promatis.Net.UI.Components.BaseTree;

/// <summary>
/// Универсальный ОЗУ-движок для инкрементального управления иерархиями MudBlazor 9.4 без перезапросов СУБД.
/// </summary>
public class InMemoryTreeEngine<TEntity> where TEntity : class
{
    private readonly Func<TEntity, Guid> _idSelector;
    private readonly Func<TEntity, Guid?> _parentIdSelector;
    private readonly Action<TEntity, TEntity> _syncNavigation;
    private readonly Action<TEntity> _clearChildren;
    private readonly Action<TEntity, TEntity> _removeChildAction; // ДОБАВЛЕНО: Правило удаления из доменной коллекции

    public List<TreeItemData<TEntity>> RootNodes { get; private set; } = [];

    public InMemoryTreeEngine(
        Func<TEntity, Guid> idSelector,
        Func<TEntity, Guid?> parentIdSelector,
        Action<TEntity, TEntity> syncNavigation,
        Action<TEntity> clearChildren,
        Action<TEntity, TEntity> removeChildAction) // Добавлено в конструктор
    {
        _idSelector = idSelector;
        _parentIdSelector = parentIdSelector;
        _syncNavigation = syncNavigation;
        _clearChildren = clearChildren;
        _removeChildAction = removeChildAction; // Фиксируем правило
    }

    /// <summary>
    /// Первичная сборка графа в памяти на основе плоского списка из СУБД.
    /// </summary>
    public void Initialize(List<TEntity> allItems)
    {
        ILookup<Guid?, TEntity> lookup = allItems.ToLookup(x => _parentIdSelector(x));
        List<TEntity> roots = lookup[null].ToList();
        RootNodes = roots.Select(r => BuildNode(r, lookup)).ToList();
    }

    private TreeItemData<TEntity> BuildNode(TEntity current, ILookup<Guid?, TEntity> lookup)
    {
        var uiItem = new TreeItemData<TEntity> { Value = current, Expanded = false };
        List<TEntity> domainChildren = lookup[_idSelector(current)].ToList();

        _clearChildren(current);

        if (domainChildren.Any())
        {
            var uiChildren = new List<TreeItemData<TEntity>>();
            foreach (TEntity child in domainChildren)
            {
                _syncNavigation(current, child);
                uiChildren.Add(BuildNode(child, lookup));
            }
            uiItem.Children = uiChildren;
        }
        return uiItem;
    }

    /// <summary>
    /// Точечно применяет дельту из СУБД-интерцептора к ОЗУ-графу.
    /// </summary>
    public void ApplyDelta(EntityStateChangeEnum state, TEntity entity)
    {
        switch (state)
        {
            case EntityStateChangeEnum.Added:
                var newUiItem = new TreeItemData<TEntity> { Value = entity, Expanded = false };
                Guid? parentId = _parentIdSelector(entity);

                if (parentId == null)
                {
                    RootNodes.Add(newUiItem);
                    return;
                }

                TreeItemData<TEntity>? parentUiNode = FindById(RootNodes, parentId.Value);
                if (parentUiNode != null)
                {
                    parentUiNode.Children ??= new List<TreeItemData<TEntity>>();
                    List<TreeItemData<TEntity>> list = parentUiNode.Children.Cast<TreeItemData<TEntity>>().ToList();
                    list.Add(newUiItem);
                    parentUiNode.Children = list;

                    if (parentUiNode.Value != null) _syncNavigation(parentUiNode.Value, entity);
                    parentUiNode.Expanded = true;
                }
                break;

            case EntityStateChangeEnum.Modified:
                TreeItemData<TEntity>? targetUiNode = FindById(RootNodes, _idSelector(entity));
                if (targetUiNode?.Value != null)
                {
                    TEntity currentUnit = targetUiNode.Value;
                    IEnumerable<PropertyInfo> properties = typeof(TEntity).GetProperties()
                        .Where(p => p.CanWrite && p.CanRead)
                        .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string));

                    foreach (PropertyInfo prop in properties)
                    {
                        prop.SetValue(currentUnit, prop.GetValue(entity));
                    }
                }
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                Guid entityId = _idSelector(entity);
                TreeItemData<TEntity>? rootMatch = RootNodes.FirstOrDefault(x => x.Value != null && _idSelector(x.Value) == entityId);
                if (rootMatch != null)
                {
                    RootNodes.Remove(rootMatch);
                    return;
                }

                TreeItemData<TEntity>? parentNode = FindParentByChildId(RootNodes, entityId);
                if (parentNode?.Children != null)
                {
                    List<TreeItemData<TEntity>> list = parentNode.Children.Cast<TreeItemData<TEntity>>().ToList();
                    TreeItemData<TEntity>? itemToRemove = list.FirstOrDefault(x => x.Value != null && _idSelector(x.Value) == entityId);
                    if (itemToRemove != null)
                    {
                        list.Remove(itemToRemove);
                        parentNode.Children = list;

                        // ИСПРАВЛЕНО: Используем строго универсальное лямбда-выражение вместо жесткого .Children
                        if (parentNode.Value != null && itemToRemove.Value != null)
                        {
                            _removeChildAction(parentNode.Value, itemToRemove.Value);
                        }
                    }
                }
                break;
        }
    }

    private TreeItemData<TEntity>? FindById(List<TreeItemData<TEntity>> nodes, Guid id)
    {
        if (nodes == null) return null;
        foreach (TreeItemData<TEntity> node in nodes)
        {
            if (node.Value != null && _idSelector(node.Value) == id) return node;
            if (node.Children != null)
            {
                TreeItemData<TEntity>? found = FindById(node.Children.Cast<TreeItemData<TEntity>>().ToList(), id);
                if (found != null) return found;
            }
        }
        return null;
    }

    private TreeItemData<TEntity>? FindParentByChildId(List<TreeItemData<TEntity>> nodes, Guid childId)
    {
        if (nodes == null) return null;
        foreach (TreeItemData<TEntity> node in nodes)
        {
            if (node.Children != null)
            {
                List<TreeItemData<TEntity>> list = node.Children.Cast<TreeItemData<TEntity>>().ToList();
                if (list.Any(x => x.Value != null && _idSelector(x.Value) == childId)) return node;
                TreeItemData<TEntity>? found = FindParentByChildId(list, childId);
                if (found != null) return found;
            }
        }
        return null;
    }
}