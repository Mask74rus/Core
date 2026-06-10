using MudBlazor;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактный табличный контекст платформы.
/// Инкапсулирует в себе универсальный математический конвейер динамической фильтрации, сортировки и пагинации MudBlazor.
/// </summary>
/// <typeparam name="TEntity">Класс бизнес-сущности таблицы (например, User, Client).</typeparam>
public abstract class GridContext<TEntity> : EntityContext<TEntity, GridState<TEntity>, GridData<TEntity>>
    where TEntity : class, new()
{
    /// <summary>
    /// ТИХИЙ КОНСТРУКТОР ТАБЛИЧНОГО ЯДРА.
    /// </summary>
    protected GridContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    /// <summary>
    /// Возвращает пустой дефолтный стейт для защиты MudBlazor от null в фазе до загрузки транспорта.
    /// </summary>
    protected override GridData<TEntity> GetEmptyDataState()
        => new GridData<TEntity> { Items = Array.Empty<TEntity>(), TotalItems = 0 };

    /// <summary>
    /// ДОМЕННЫЙ ФИЛЬТР ДЛЯ НАСЛЕДНИКОВ.
    /// Переопределяется конкретным справочником для жесткого системного отсечения строк (права, архивы)
    /// БЕЗ риска нарушить потокобезопасность ОЗУ-кэша и БЕЗ копипаста рутины пагинации.
    /// </summary>
    protected virtual Func<TEntity, bool>? GetDomainFilter() => null;

    /// <summary>
    /// ИСТИННАЯ ОТВЕТСТВЕННОСТЬ ТАБЛИЦЫ: Математический конвейер обработки данных.
    /// Сделан virtual, чтобы тяжелые или специфичные экраны могли переопределить логику фильтрации колонок.
    /// Передается ссылкой в метод ConfigureUsingInMemoryMode при сборке конечного контекста.
    /// </summary>
    protected virtual GridData<TEntity> EvaluateDataStateInMemory(IReadOnlyList<TEntity> inMemoryList, GridState<TEntity> state)
    {
        // 1. Сначала нативно применяем доменный фильтр наследника, если он задан прикладным программистом
        var domainFilter = GetDomainFilter();
        IEnumerable<TEntity> filteredList = domainFilter != null
            ? inMemoryList.Where(domainFilter)
            : inMemoryList;

        // Превращаем отфильтрованный поток в LINQ-выражение для динамических колонок MudBlazor
        IQueryable<TEntity> query = filteredList.AsQueryable();

        // 2. Динамическая фильтрация колонок самого интерфейса MudBlazor (выполняется на стороне клиента мгновенно)
        if (state.FilterDefinitions != null && state.FilterDefinitions.Any())
        {
            query = query.Where(state.FilterDefinitions);
        }

        // 3. Динамическая сортировка колонок самого интерфейса MudBlazor
        if (state.SortDefinitions != null && state.SortDefinitions.Any())
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