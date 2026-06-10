using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактный древовидный контекст платформы.
/// Нативно инкапсулирует математический конвейер линейного упорядочивания и фильтрации иерархических структур графа.
/// </summary>
/// <typeparam name="TEntity">Класс бизнес-сущности дерева, обязанный соблюдать контракт ITreeNode.</typeparam>
public abstract class TreeContext<TEntity> : EntityContext<TEntity, object, List<TEntity>>
    where TEntity : class, ITreeNode<TEntity>, new()
{
    /// <summary>
    /// Готовый, собранный за O(N) рекурсивный граф элементов для прямой привязки к параметру RootItems пассивного MudTreeView.
    /// </summary>
    public List<TreeItemData<TEntity>> TreeGraph { get; private set; } = [];

    /// <summary>
    /// ТИХИЙ КОНСТРУКТОР ДРЕВОВИДНОГО ЯДРА.
    /// </summary>
    protected TreeContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Извлекаем и жестко связываем специализированную стратегию древовидных мутаций графа ОЗУ
        var treeStrategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(treeStrategy);
    }

    protected override List<TEntity> GetEmptyDataState() => [];

    /// <summary>
    /// ДОМЕННЫЙ ФИЛЬТР ДЛЯ НАСЛЕДНИКОВ ИЕРАРХИИ.
    /// </summary>
    protected virtual Func<TEntity, bool>? GetDomainFilter() => null;

    /// <summary>
    /// ПЕРЕОПРЕДЕЛЕНИЕ ЕДИНОЙ ТОЧКИ ЗАПРОСА ДАННЫХ.
    /// Контекст перехватывает плоский список Брокера, сам собирает из него TreeGraph и обновляет стейт UI.
    /// </summary>
    public override async Task<List<TEntity>> GetDataAsync(object state, CancellationToken ct = default)
    {
        // 1. Извлекаем плоский упорядоченный список строк из Брокера (или Ozu-кэша)
        List<TEntity> flatList = await base.GetDataAsync(state, ct);

        // 2. Инфраструктура ядра сама собирает рекурсивный граф, освобождая от этого визуальный слой страницы!
        TreeGraph = BuildTreeGraphInternal(flatList);

        return flatList;
    }

    /// <summary>
    /// ЖЕСТКИЙ МАТЕМАТИЧЕСКИЙ КОНВЕЙЕР ОБРАБОТКИ: Упорядочивание графа в памяти.
    /// Корневые элементы гарантированно идут первыми для безошибочной сборки дерева.
    /// </summary>
    protected List<TEntity> EvaluateDataStateInMemory(IReadOnlyList<TEntity> inMemoryList, object state)
    {
        var domainFilter = GetDomainFilter();
        IEnumerable<TEntity> filteredList = domainFilter != null
            ? inMemoryList.Where(domainFilter)
            : inMemoryList;

        return filteredList
            .OrderBy(x => x.ParentId != null)
            .ToList();
    }

    /// <summary>
    /// Универсальный алгоритм построения дерева за линейное время O(N) на базе вашего контракта ITreeNode.
    /// Перенесен со страницы в бизнес-слой данных для соблюдения Separation of Concerns.
    /// </summary>
    private List<TreeItemData<TEntity>> BuildTreeGraphInternal(List<TEntity> flatList)
    {
        if (flatList == null || !flatList.Any()) return [];

        // 1. Промежуточная хэш-карта мутабельных рабочих списков детей
        var nodeEntries = flatList.ToDictionary(
            x => x.Id,
            x => new {
                ItemData = new TreeItemData<TEntity> { Value = x },
                MutableChildren = new List<TreeItemData<TEntity>>()
            }
        );

        var rootNodes = new List<TreeItemData<TEntity>>();

        // 2. Распределяем узлы по родителям за один плоский проход O(N)
        foreach (var entry in nodeEntries.Values)
        {
            TEntity currentEntity = entry.ItemData.Value!;

            if (currentEntity.ParentId == null || !nodeEntries.TryGetValue(currentEntity.ParentId.Value, out var parentEntry))
            {
                rootNodes.Add(entry.ItemData); // Узел корневой
            }
            else
            {
                parentEntry.MutableChildren.Add(entry.ItemData); // Узел подчиненный
            }
        }

        // 3. Проставляем готовые списки детей в свойства TreeItemData
        foreach (var entry in nodeEntries.Values)
        {
            if (entry.MutableChildren.Any())
            {
                entry.ItemData.Children = entry.MutableChildren;
            }
        }

        return rootNodes;
    }
}