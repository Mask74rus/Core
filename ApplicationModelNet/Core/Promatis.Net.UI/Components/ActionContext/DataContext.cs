using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактное ядро работы со сквозными потоками данных доменной сущности.
/// Координирует работу универсального Брокера и ОЗУ-кэша, отвечая за полиморфный флаг IsLoading и единый пульс.
/// </summary>
public abstract class DataContext<TEntity, TQueryState, TResultData> : WorkspaceContext
    where TEntity : class, new()
{
    private bool _isLoading;

    /// <summary>
    /// Универсальный диспетчер конвейера данных текущего экрана (InMemory / Server).
    /// </summary>
    public IUiDataBroker<TEntity, TQueryState, TResultData> Broker { get; }

    /// <summary>
    /// Высокопроизводительный ОЗУ-кэш живых объектов домена.
    /// </summary>
    public IUiOzuCache<TEntity> OzuCache { get; }

    /// <summary>
    /// Честное полиморфное переопределение свойства базовой геометрии холста.
    /// Пассивный каркас WorkspacePage нативно считает его состояние через каскад параметров Blazor.
    /// </summary>
    public override bool IsLoading => _isLoading;

    /// <summary>
    /// ЕДИНЫЙ ОТКРЫТЫЙ ПУЛЬС ХОЛСТА И ДАННЫХ.
    /// Сигнализирует о ЛЮБЫХ изменениях состояния (загрузка, триггеры СУБД, мутации ОЗУ).
    /// Обобщенная страница ядра (ReferencePageBase) подпишется на него централизованно в одной точке.
    /// </summary>
    public event Action? OnContextUpdated;

    /// <summary>
    /// Универсальный метод вызова пульса обновления для всех нижестоящих потомков и Брокера.
    /// </summary>
    public void NotifyContextUpdated() => OnContextUpdated?.Invoke();

    protected DataContext(IServiceProvider serviceProvider, bool isInMemoryMode) : base()
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Извлекаем Transient-компоненты автономного стейт-движка из DI сессии
        OzuCache = serviceProvider.GetRequiredService<IUiOzuCache<TEntity>>();
        Broker = serviceProvider.GetRequiredService<IUiDataBroker<TEntity, TQueryState, TResultData>>();

        // Конфигурируем Брокер прямо в конструкторе — ликвидирует гонки состояний при ленивой загрузке.
        if (isInMemoryMode)
        {
            Broker.ConfigureInMemoryMode(OzuCache, LoadInitialDataForBrokerAsync, EvaluateDataStateInMemory);
        }
        else
        {
            Broker.ConfigureServerMode(FetchDataFromServerAsync);
        }
    }

    /// <summary>
    /// ЕДИНАЯ И УНИВЕРСАЛЬНАЯ ТОЧКА ЗАПРОСА ДАННЫХ ДЛЯ ЛЮБЫХ UI-ТАБЛИЦ И ДЕРЕВЬЕВ ПЛАТФОРМЫ.
    /// Безопасно оборачивает выполнение Брокера в индикацию загрузки и бьет в колокол OnContextUpdated.
    /// </summary>
    public virtual async Task<TResultData> GetDataAsync(TQueryState state, CancellationToken ct = default)
    {
        if (_isLoading) return default!;

        try
        {
            _isLoading = true;
            NotifyContextUpdated(); // Импульс 1: Включаем MudOverlay на экране через единое событие

            return await Broker.FetchDataAsync(state, ct);
        }
        finally
        {
            _isLoading = false;
            NotifyContextUpdated(); // Импульс 2: Выключаем MudOverlay на экране через единое событие
        }
    }

    // --- ОБЯЗАТЕЛЬНЫЕ ФУНКЦИОНАЛЬНЫЕ МОСТЫ ДЛЯ ПОТОМКОВ ЯДРА ---
    protected abstract Task<List<TEntity>> LoadInitialDataForBrokerAsync(TQueryState state, CancellationToken ct);
    protected abstract TResultData EvaluateDataStateInMemory(TQueryState state, IReadOnlyList<TEntity> inMemoryList);
    protected abstract Task<TResultData> FetchDataFromServerAsync(TQueryState state, CancellationToken ct);
}