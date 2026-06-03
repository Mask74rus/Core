namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для древовидных страниц (Tree Mode).
/// Переводит абстрактные данные ядра на язык иерархических визуализаторов.
/// </summary>
public abstract class TreeWorkspaceContext<TEntity, TKey>
    : EntityWorkspaceContext<TEntity, TKey, object, List<TEntity>>
    where TEntity : class, new()
    where TKey : notnull
{
    protected TreeWorkspaceContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        IOzuMutationStrategy<TEntity> treeStrategy,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        if (treeStrategy == null) throw new ArgumentNullException(nameof(treeStrategy));

        // Переключаем плоский ОЗУ-кэш ядра на строго типизированную иерархическую стратегию мутаций
        OzuCache.SetMutationStrategy(treeStrategy);
    }

    /// <summary>
    /// Для дерева в памяти мы просто возвращаем весь накопленный граф объектов.
    /// </summary>
    protected override List<TEntity> EvaluateDataStateInMemory(object state, List<TEntity> inMemoryList)
    {
        return inMemoryList;
    }

    /// <summary>
    /// Честный серверный запрос для дерева. Загружает весь линейный список записей из СУБД,
    /// на основе которого визуализатор построит граф связей ParentId -> Children.
    /// </summary>
    protected override async Task<List<TEntity>> FetchDataFromServerAsync(object state, CancellationToken ct)
    {
        return await GetBaseService().GetAllAsync();
    }
}