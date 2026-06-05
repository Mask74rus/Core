using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для древовидных страниц (Tree Mode).
/// Переводит абстрактные данные ядра на язык иерархических визуализаторов.
/// </summary>
public abstract class TreeContext<TEntity, TKey> : EntityContext<TEntity, TKey, object, List<TEntity>>
    where TEntity : class, new()
    where TKey : notnull
{
    protected TreeContext(IServiceProvider serviceProvider, bool isInMemoryMode)
        : base(serviceProvider, isInMemoryMode)
    {
        // ВНИМАНИЕ (Иерархическая физика): Извлекаем и жестко связываем из DI 
        // специализированную стратегию древовидных мутаций графа ОЗУ!
        var treeStrategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(treeStrategy);
    }

    /// <summary>
    /// ДОМЕННЫЙ ФИЛЬТР ДЛЯ НАСЛЕДНИКОВ ИЕРАРХИИ.
    /// Переопределяется конкретным деревом для жесткого отсечения узлов графа (например, по правам доступа),
    /// БЕЗ риска нарушить потокобезопасность ОЗУ-кэша.
    /// </summary>
    protected virtual Func<TEntity, bool>? GetDomainFilter() => null;

    /// <summary>
    /// ИСТИННАЯ ОТВЕТСТВЕННОСТЬ ДЕРЕВА: Конвейер обработки иерархического списка элементов в памяти.
    /// Принимает безопасный IReadOnlyList, изолированный Брокером внутри потокобезопасного ExecuteInLock.
    /// </summary>
    protected override List<TEntity> EvaluateDataStateInMemory(object state, IReadOnlyList<TEntity> inMemoryList)
    {
        // 1. Применяем доменный фильтр наследника, если он задан (например, скрыть архивные узлы)
        var domainFilter = GetDomainFilter();
        IEnumerable<TEntity> filteredList = domainFilter != null
            ? inMemoryList.Where(domainFilter)
            : inMemoryList;

        // 2. Иерархическое упорядочивание графа. 
        // По умолчанию мы возвращаем линейный список, но отсортированный так, чтобы корневые элементы 
        // (у которых иерархическая связь пуста) шли первыми. Конкретное дерево может уточнить этот маппинг.
        return filteredList
            .OrderBy(x => x.GetType().GetProperty("ParentId")?.GetValue(x) != null)
            .ToList();
    }
}