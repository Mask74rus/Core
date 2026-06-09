using MudBlazor;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Промежуточный контекст представления для табличных страниц (Grid Mode).
/// Мапит абстрактные данные ядра в строго типизированные структуры пагинации MudBlazor (GridState и GridData).
/// </summary>
public abstract class GridContext<TEntity, TKey> : EntityContext<TEntity, TKey, GridState<TEntity>, GridData<TEntity>>
    where TEntity : class, new()
    where TKey : notnull
{
    protected GridContext(IServiceProvider serviceProvider, bool isInMemoryMode)
        : base(serviceProvider, isInMemoryMode)
    {
    }

    protected override GridData<TEntity> GetEmptyDataState()
        => new GridData<TEntity> { Items = [], TotalItems = 0 };

    /// <summary>
    /// ДОМЕННЫЙ ФИЛЬТР ДЛЯ НАСЛЕДНИКОВ.
    /// Переопределяется конкретным справочником для жесткого отсечения строк (права, архивы)
    /// БЕЗ риска нарушить потокобезопасность ОЗУ-кэша и БЕЗ копипаста рутины пагинации.
    /// </summary>
    protected virtual Func<TEntity, bool>? GetDomainFilter() => null;

    /// <summary>
    /// ИСТИННАЯ ОТВЕТСТВЕННОСТЬ ТАБЛИЦЫ: Математический конвейер обработки данных.
    /// Принимает безопасный IReadOnlyList, изолированный Брокером внутри потокобезопасного ExecuteInLock.
    /// </summary>
    protected override GridData<TEntity> EvaluateDataStateInMemory(GridState<TEntity> state, IReadOnlyList<TEntity> inMemoryList)
    {
        // 1. Сначала нативно применяем доменный фильтр наследника, если он задан
        var domainFilter = GetDomainFilter();
        IEnumerable<TEntity> filteredList = domainFilter != null
            ? inMemoryList.Where(domainFilter)
            : inMemoryList;

        // Превращаем отфильтрованный поток в LINQ-выражение для MudBlazor колонок
        IQueryable<TEntity> query = filteredList.AsQueryable();

        // 2. Динамическая фильтрация колонок самого интерфейса MudBlazor
        if (state.FilterDefinitions.Any())
        {
            query = query.Where(state.FilterDefinitions);
        }

        // 3. Динамическая сортировка колонок самого интерфейса MudBlazor
        if (state.SortDefinitions.Any())
        {
            query = query.OrderBy(state.SortDefinitions);
        }

        // МАТЕМАТИЧЕСКИЙ ИНВАРИАНТ: Фиксируем точный тотал ОТФИЛЬТРОВАННЫХ строк ДО пагинации
        int totalCount = query.Count();

        // 4. Вырезаем фрейм конкретной страницы (Пагинация MudBlazor)
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
}