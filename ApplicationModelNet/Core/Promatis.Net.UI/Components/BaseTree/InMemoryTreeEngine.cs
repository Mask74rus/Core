using MudBlazor;
using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.UI.Components.BaseTree;

/// <summary>
/// Универсальный ОЗУ-движок для инкрементального управления иерархиями MudBlazor 9.4.
/// Полностью автоматизирован на основе интерфейса ITreeNode. Конструктор пуст.
/// </summary>
public class InMemoryTreeEngine<TEntity> where TEntity : class, ITreeNode<TEntity>
{
    public List<TreeItemData<TEntity>> RootNodes { get; private set; } = [];

    // ИСПРАВЛЕНО: Конструктор теперь абсолютно пуст! Никаких лямбд и селекторов больше нет.
    public InMemoryTreeEngine()
    {
    }

    /// <summary>
    /// Первичная сборка графа в оперативной памяти на основе плоского списка из СУБД.
    /// </summary>
    public void Initialize(List<TEntity> allItems)
    {
        // Нативно группируем по ParentId через интерфейс
        ILookup<Guid?, TEntity> lookup = allItems.ToLookup(x => x.ParentId);
        List<TEntity> roots = lookup[null].ToList();

        RootNodes = roots.Select(r => BuildNode(r, lookup)).ToList();
    }

    private TreeItemData<TEntity> BuildNode(TEntity current, ILookup<Guid?, TEntity> lookup)
    {
        var uiItem = new TreeItemData<TEntity> { Value = current, Expanded = false };

        // Нативно читаем Id через интерфейс
        List<TEntity> domainChildren = lookup[current.Id].ToList();

        // Нативно очищаем коллекцию дочерних элементов через интерфейс
        current.Children.Clear();

        if (domainChildren.Any())
        {
            var uiChildren = new List<TreeItemData<TEntity>>();
            foreach (TEntity child in domainChildren)
            {
                // Нативно синхронизируем объектные связи в ОЗУ через свойства интерфейса
                current.Children.Add(child);
                child.Parent = current;

                uiChildren.Add(BuildNode(child, lookup));
            }
            uiItem.Children = uiChildren;
        }
        return uiItem;
    }

    /// <summary>
    /// Точечно и хирургически применяет дельту транзакции СУБД к ОЗУ-графу за 0 мс.
    /// </summary>
    public void ApplyDelta(EntityStateChangeEnum state, TEntity entity)
    {
        switch (state)
        {
            case EntityStateChangeEnum.Added:
                var newUiItem = new TreeItemData<TEntity> { Value = entity, Expanded = false };
                Guid? parentId = entity.ParentId; // Читаем нативно

                if (parentId == null)
                {
                    RootNodes.Add(newUiItem);
                    return;
                }

                var parentUiNode = FindById(RootNodes, parentId.Value);
                if (parentUiNode != null)
                {
                    parentUiNode.Children ??= new List<TreeItemData<TEntity>>();
                    var list = parentUiNode.Children.Cast<TreeItemData<TEntity>>().ToList();
                    list.Add(newUiItem);
                    parentUiNode.Children = list;

                    if (parentUiNode.Value != null)
                    {
                        // Синхронизируем связи домена в ОЗУ нативно через свойства интерфейса
                        parentUiNode.Value.Children.Add(entity);
                        entity.Parent = parentUiNode.Value;
                    }
                    parentUiNode.Expanded = true;
                }
                break;

            case EntityStateChangeEnum.Modified:
                var targetUiNode = FindById(RootNodes, entity.Id); // Читаем Id нативно
                if (targetUiNode?.Value != null)
                {
                    TEntity currentUnit = targetUiNode.Value;

                    // Копируем плоские измененные поля по рефлексии
                    var properties = typeof(TEntity).GetProperties()
                        .Where(p => p.CanWrite && p.CanRead)
                        .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string));

                    foreach (var prop in properties)
                    {
                        prop.SetValue(currentUnit, prop.GetValue(entity));
                    }
                }
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                Guid entityId = entity.Id; // Читаем Id нативно
                var rootMatch = RootNodes.FirstOrDefault(x => x.Value != null && x.Value.Id == entityId);
                if (rootMatch != null)
                {
                    RootNodes.Remove(rootMatch);
                    return;
                }

                var parentNode = FindParentByChildId(RootNodes, entityId);
                if (parentNode?.Children != null)
                {
                    var list = parentNode.Children.Cast<TreeItemData<TEntity>>().ToList();
                    var itemToRemove = list.FirstOrDefault(x => x.Value != null && x.Value.Id == entityId);
                    if (itemToRemove != null)
                    {
                        list.Remove(itemToRemove);
                        parentNode.Children = list;

                        // Удаляем из доменной коллекции нативно через интерфейс
                        if (parentNode.Value != null && itemToRemove.Value != null)
                        {
                            parentNode.Value.Children.Remove(itemToRemove.Value);
                        }
                    }
                }
                break;
        }
    }

    private TreeItemData<TEntity>? FindById(List<TreeItemData<TEntity>> nodes, Guid id)
    {
        if (nodes == null) return null;
        foreach (var node in nodes)
        {
            if (node.Value != null && node.Value.Id == id) return node; // Читаем нативно
            if (node.Children != null)
            {
                var found = FindById(node.Children.Cast<TreeItemData<TEntity>>().ToList(), id);
                if (found != null) return found;
            }
        }
        return null;
    }

    private TreeItemData<TEntity>? FindParentByChildId(List<TreeItemData<TEntity>> nodes, Guid childId)
    {
        if (nodes == null) return null;
        foreach (var node in nodes)
        {
            if (node.Children != null)
            {
                var list = node.Children.Cast<TreeItemData<TEntity>>().ToList();
                if (list.Any(x => x.Value != null && x.Value.Id == childId)) return node; // Читаем нативно
                var found = FindParentByChildId(list, childId);
                if (found != null) return found;
            }
        }
        return null;
    }
}