using MudBlazor;

namespace Promatis.Net.UI;

/// <summary>
/// Платформенный брокер данных. Централизованно управляет поставкой данных для визуализаторов.
/// Полностью абстрагирует UI от конкретных интерфейсов доменных служб бэкенда.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности</typeparam>
public class UiDataBroker<TEntity> where TEntity : class
{
    // Делегат для подключения кастомных бэкенд-провайдеров (например, постраничного поиска логов)
    private Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? _customServerDataProvider;

    /// <summary>
    /// Внутреннее кэш-хранилище плоской коллекции для CRUD-справочников в ОЗУ.
    /// </summary>
    public List<TEntity>? InMemoryItems { get; set; }

    /// <summary>
    /// Возвращает истину, если брокер переведен в режим работы с локальной ОЗУ-коллекцией.
    /// </summary>
    public bool IsInMemoryMode => InMemoryItems != null;

    /// <summary>
    /// Явная инициализация брокера кастомным серверным поставщиком данных (для тяжелых реестров, CQRS, Search-запросов).
    /// </summary>
    public void ConfigureServerMode(Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>> dataProvider)
    {
        _customServerDataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        InMemoryItems = null; // Сбрасываем ОЗУ-режим, если он был включен
    }

    /// <summary>
    /// Явная инициализация брокера готовым плоским списком (для простых справочников в памяти браузера).
    /// </summary>
    public void ConfigureInMemoryMode(List<TEntity> items)
    {
        InMemoryItems = items ?? throw new ArgumentNullException(nameof(items));
        _customServerDataProvider = null; // Сбрасываем серверный режим
    }

    /// <summary>
    /// Главный унифицированный метод запроса данных, который будут вызывать Grid, Tree или Мнемосхема.
    /// Хирургически точно распределяет вызов в зависимости от настроенного режима.
    /// </summary>
    public async Task<GridData<TEntity>> FetchDataAsync(GridState<TEntity> state, CancellationToken ct = default)
    {
        // Сценарий А: Включен кастомный серверный режим (CQRS / Поиск по фильтрам дат)
        if (_customServerDataProvider != null)
        {
            return await _customServerDataProvider(state, ct);
        }

        // Сценарий Б: Включен ОЗУ-режим локальной коллекции (Простые MES/MDM справочники)
        if (InMemoryItems != null)
        {
            // На данном микро-шаге просто возвращаем коллекцию. Логику ОЗУ-сортировки и пагинации
            // на клиенте мы добавим позже, когда вернем в грид ОЗУ-движок.
            return new GridData<TEntity>
            {
                Items = InMemoryItems,
                TotalItems = InMemoryItems.Count
            };
        }

        // Дефолтный пустой ответ, если брокер еще не успели настроить
        return new GridData<TEntity> { Items = Array.Empty<TEntity>(), TotalItems = 0 };
    }
}