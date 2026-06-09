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
    private bool _isTransportActivated; // Замок активации Брокера
    private readonly bool _isInMemoryMode; // Сохраняем признак режима работы

    public IUiDataBroker<TEntity, TQueryState, TResultData> Broker { get; }
    public IUiOzuCache<TEntity> OzuCache { get; }

    public override bool IsLoading => _isLoading;

    /// <summary>
    /// Сигнализирует о том, что фаза ленивой инициализации Брокера завершена и экран готов к выводу данных.
    /// </summary>
    public bool IsTransportActivated => _isTransportActivated;

    public event Action? OnContextUpdated;
    public void NotifyContextUpdated() => OnContextUpdated?.Invoke();

    /// <summary>
    /// ТИХИЙ КОНСТРУКТОР ЯДРА.
    /// Выполняется за 0 наносекунд. Только выделяет память и сохраняет IoC-ссылки.
    /// </summary>
    protected DataContext(IServiceProvider serviceProvider, bool isInMemoryMode) : base()
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        _isInMemoryMode = isInMemoryMode;
        OzuCache = serviceProvider.GetRequiredService<IUiOzuCache<TEntity>>();
        Broker = serviceProvider.GetRequiredService<IUiDataBroker<TEntity, TQueryState, TResultData>>();
    }

    /// <summary>
    /// ЯВНАЯ АКТИВАЦИЯ БИЗНЕС-ТРАНСПОРТА.
    /// Вызывается прикладной страницей из OnAfterRenderAsync, когда разметка гарантированно отрисована.
    /// </summary>
    public async Task ActivateTransportAsync()
    {
        if (_isTransportActivated) return;

        // Конфигурируем Брокер строго в фазе ленивой загрузки экрана
        if (_isInMemoryMode)
        {
            Broker.ConfigureInMemoryMode(OzuCache, LoadInitialDataForBrokerAsync, EvaluateDataStateInMemory);
        }
        else
        {
            Broker.ConfigureServerMode(FetchDataFromServerAsync);
        }

        // Вызываем виртуальный метод дозагрузки метаданных фильтров (например, для логов аудита)
        await LoadMetadataInternalAsync();

        _isTransportActivated = true;
    }

    /// <summary>
    /// Виртуальный хук ядра для ленивой асинхронной подгрузки метаданных фильтров справочников.
    /// </summary>
    protected virtual Task LoadMetadataInternalAsync() => Task.CompletedTask;

    protected abstract TResultData GetEmptyDataState();

    /// <summary>
    /// ЕДИНАЯ ТОЧКА ЗАПРОСА ДАННЫХ.
    /// </summary>
    public virtual async Task<TResultData> GetDataAsync(TQueryState state, CancellationToken ct = default)
    {
        if (!_isTransportActivated)
            return default!;

        // Защита от повторных спам-кликов, пока таск выполняется в фоне
        if (_isLoading)
            return default!;

        try
        {
            _isLoading = true;

            // ВНИМАНИЕ: Отсюда полностью УДАЛЕН вызов NotifyContextUpdated()!
            // Мы больше не бьем в колокол ДО получения данных. 
            // Мы даем Брокеру спокойно и в тишине скачать строки из PostgreSQL.

            TResultData result = await Broker.FetchDataAsync(state, ct);

            // Возвращаем данные. Сначала MudBlazor примет их во внутренние поля, 
            // настроит итераторы страниц, и только ПОСЛЕ этого завершит таск!
            return result;
        }
        finally
        {
            _isLoading = false;

            // БЬЕМ В КОЛОКОЛ СТРОГО ТУТ: Данные уже гарантированно внутри MudBlazor,
            // оверлей загрузки IsLoading гаснет, и страница каскадно и безопасно 
            // перерисовывает кнопки тулбара на основе уже прилетевших живых строк!
            NotifyContextUpdated();
        }
    }

    protected abstract Task<List<TEntity>> LoadInitialDataForBrokerAsync(TQueryState state, CancellationToken ct);
    protected abstract TResultData EvaluateDataStateInMemory(TQueryState state, IReadOnlyList<TEntity> inMemoryList);
    protected abstract Task<TResultData> FetchDataFromServerAsync(TQueryState state, CancellationToken ct);
}