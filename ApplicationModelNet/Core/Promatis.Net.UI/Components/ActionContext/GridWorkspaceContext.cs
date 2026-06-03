using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для табличных страниц (Grid Mode).
/// Мапит абстрактные данные ядра в строго типизированные структуры пагинации MudBlazor (GridState и GridData).
/// </summary>
public abstract class GridWorkspaceContext<TEntity, TKey>
    : EntityWorkspaceContext<TEntity, TKey, GridState<TEntity>, GridData<TEntity>>
    where TEntity : class, new()
    where TKey : notnull
{
    protected GridWorkspaceContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        // По умолчанию для табличного представления фиксируем плоскую стратегию мутаций ОЗУ-кэша
        OzuCache.SetMutationStrategy(new FlatOzuMutationStrategy<TEntity>());
    }

    /// <summary>
    /// Реализация локальной табличной пагинации в оперативной памяти (InMemory Mode).
    /// </summary>
    protected override GridData<TEntity> EvaluateDataStateInMemory(GridState<TEntity> state, List<TEntity> inMemoryList)
    {
        return new GridData<TEntity>
        {
            Items = inMemoryList.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList(),
            TotalItems = inMemoryList.Count
        };
    }

    /// <summary>
    /// Реализация честного серверного постраничного запроса к СУБД (Server Mode).
    /// </summary>
    protected override async Task<GridData<TEntity>> FetchDataFromServerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        // Вызываем базовый сервис данных, транслируя стейт MudBlazor в параметры пагинации API
        PagedResult<TEntity> pagedResult = await GetBaseService().GetPagedAsync(state.Page, state.PageSize, ct);

        return new GridData<TEntity>
        {
            Items = pagedResult.Items,
            TotalItems = pagedResult.TotalCount
        };
    }
}