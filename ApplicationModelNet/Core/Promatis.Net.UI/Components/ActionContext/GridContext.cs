using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для табличных страниц (Grid Mode).
/// Мапит абстрактные данные ядра в строго типизированные структуры пагинации MudBlazor (GridState и GridData).
/// </summary>
public abstract class GridContext<TEntity, TKey>
    : EntityContext<TEntity, TKey, GridState<TEntity>, GridData<TEntity>>
    where TEntity : class, new()
    where TKey : notnull
{
    protected GridContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
        // ИСПРАВЛЕНО: Инверсия зависимостей. Извлекаем стратегию мутаций ОЗУ через DI
        var strategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(strategy);
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Реализация локальной фильтрации, сортировки и пагинации в памяти.
    /// Метод принимает безопасный IReadOnlyList и использует нативные методы MudBlazor для IQueryable.
    /// </summary>
    protected override GridData<TEntity> EvaluateDataStateInMemory(GridState<TEntity> state, IReadOnlyList<TEntity> inMemoryList)
    {
        // Превращаем безопасный список в LINQ-выражение
        IQueryable<TEntity> query = inMemoryList.AsQueryable();

        // 1. Применяем пользовательские фильтры колонок MudBlazor на лету
        if (state.FilterDefinitions != null && state.FilterDefinitions.Any())
        {
            query = query.Where(state.FilterDefinitions);
        }

        // 2. Применяем динамическую сортировку колонок MudBlazor
        if (state.SortDefinitions != null && state.SortDefinitions.Any())
        {
            query = query.OrderBy(state.SortDefinitions);
        }

        // Вычисляем общее количество записей, прошедших фильтрацию, до разделения на страницы
        int totalCount = query.Count();

        // 3. Вырезаем текущую страницу данных (Пагинация)
        List<TEntity> pagedItems = query
            .Skip(state.Page * state.PageSize)
            .Take(state.PageSize)
            .ToList();

        return new GridData<TEntity>
        {
            Items = pagedItems,
            TotalItems = totalCount
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