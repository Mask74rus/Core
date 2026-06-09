using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для древовидных страниц (Tree Mode).
/// Переводит абстрактные данные ядра на язык иерархических визуализаторов.
/// </summary>
public abstract class TreeContext<TEntity> : EntityContext<TEntity, Guid, object, List<TEntity>>
    where TEntity : class, ITreeNode<TEntity>, new()
{
    protected TreeContext(IServiceProvider serviceProvider, bool isInMemoryMode)
        : base(serviceProvider, isInMemoryMode)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // ВНИМАНИЕ: Извлекаем и жестко связываем специализированную стратегию 
        // древовидных мутаций графа ОЗУ силами потокобезопасного кэша.
        var treeStrategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(treeStrategy);
    }

    protected override List<TEntity> GetEmptyDataState()
        => new List<TEntity>();

    /// <summary>
    /// ДОМЕННЫЙ ФИЛЬТР ДЛЯ НАСЛЕДНИКОВ ИЕРАРХИИ.
    /// Переопределяется конкретным деревом для жесткого отсечения узлов графа (права, архивы)
    /// БЕЗ риска нарушить потокобезопасность ОЗУ-кэша и без дублирования конвейера сортировки.
    /// </summary>
    protected virtual Func<TEntity, bool>? GetDomainFilter() => null;

    /// <summary>
    /// ИСТИННАЯ ОТВЕТСТВЕННОСТЬ ДЕРЕВА: Конвейер обработки иерархического списка элементов в памяти.
    /// Принимает безопасный IReadOnlyList, изолированный Брокером внутри потокобезопасного ExecuteInLock.
    /// </summary>
    protected override List<TEntity> EvaluateDataStateInMemory(object state, IReadOnlyList<TEntity> inMemoryList)
    {
        // 1. Применяем жесткий доменный фильтр наследника, если он задан (например, скрыть архивные узлы)
        var domainFilter = GetDomainFilter();
        IEnumerable<TEntity> filteredList = domainFilter != null
            ? inMemoryList.Where(domainFilter)
            : inMemoryList;

        // 2. Иерархическое упорядочивание графа без рантайм-рефлексии.
        // Корневые элементы (у которых ParentId == null) гарантированно идут первыми 
        // в плоском массиве для безошибочной ленивой сборки рекурсивного дерева на прикладной странице.
        return filteredList
            .OrderBy(x => x.ParentId != null)
            .ToList();
    }
}