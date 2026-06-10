using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактное ядро работы со сквозными потоками данных доменной сущности.
/// Координирует работу универсального Брокера и ОЗУ-кэша, отвечая за полиморфный флаг IsLoading и единый пульс.
/// </summary>
public abstract class DataContext<TEntity, TQueryState, TResultData> : WorkspaceContext, IDataContext
    where TEntity : class, new()
{
    private bool _isLoading;
    private bool _isTransportActivated;

    // Внутренние делегаты — скрытая физика ядра. Наличие одного из них автоматически определяет режим.
    private Func<CancellationToken, Task<List<TEntity>>>? _downloadAllDelegate;
    private Func<IReadOnlyList<TEntity>, TQueryState, TResultData>? _filterInMemoryDelegate;
    private Func<TQueryState, CancellationToken, Task<TResultData>>? _downloadPageDelegate;

    public IUiOzuCache<TEntity> OzuCache { get; }
    public IUiDataBroker<TEntity, TQueryState, TResultData> Broker { get; }

    /// <summary>
    /// Экран заблокирован, пока идет фоновый запрос или транспорт еще не встал в строй.
    /// </summary>
    public override bool IsLoading => _isLoading || !_isTransportActivated;

    public bool IsTransportActivated => _isTransportActivated;

    public event Action? OnContextUpdated;

    public virtual void NotifyContextUpdated() => OnContextUpdated?.Invoke();

    /// <summary>
    /// ТИХИЙ КОНСТРУКТОР ЯДРА.
    /// Больше не принимает никаких флагов. Только чистый IoC/DI.
    /// </summary>
    protected DataContext(IServiceProvider serviceProvider) : base()
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        OzuCache = serviceProvider.GetRequiredService<IUiOzuCache<TEntity>>();
        Broker = serviceProvider.GetRequiredService<IUiDataBroker<TEntity, TQueryState, TResultData>>();
    }

    // --- СЕКЦИЯ НАСТРОЙКИ РЕЖИМА ЧЕРЕЗ НАЛИЧИЕ ДЕЛЕГАТОВ (БЕЗ ФЛАГОВ) ---

    /// <summary>
    /// Конфигурирует работу контекста через оперативную память (In-Memory режим).
    /// </summary>
    protected void ConfigureUsingInMemoryMode(
        Func<CancellationToken, Task<List<TEntity>>> downloadAll,
        Func<IReadOnlyList<TEntity>, TQueryState, TResultData> filter)
    {
        _downloadAllDelegate = downloadAll ?? throw new ArgumentNullException(nameof(downloadAll));
        _filterInMemoryDelegate = filter ?? throw new ArgumentNullException(nameof(filter));
        _downloadPageDelegate = null; // Гарантируем атомарность выбора режима
    }

    /// <summary>
    /// Конфигурирует работу контекста напрямую через gRPC-запросы к серверу (Server-Side режим).
    /// </summary>
    protected void ConfigureUsingServerSideMode(
        Func<TQueryState, CancellationToken, Task<TResultData>> downloadPage)
    {
        _downloadPageDelegate = downloadPage ?? throw new ArgumentNullException(nameof(downloadPage));
        _downloadAllDelegate = null;
        _filterInMemoryDelegate = null;
    }

    /// <summary>
    /// ЯВНАЯ АКТИВАЦИЯ БИЗНЕС-ТРАНСПОРТА.
    /// Брокер конфигурируется автоматически на основе того, какой делегат был инициализирован в конструкторе.
    /// </summary>
    public async Task ActivateTransportAsync()
    {
        if (_isTransportActivated) return;

        // Физика рантайма: проверяем, какой метод конфигурации вызвал наследник в своем конструкторе
        if (_downloadAllDelegate != null && _filterInMemoryDelegate != null)
        {
            Broker.ConfigureInMemoryMode(
                OzuCache,
                _downloadAllDelegate,
                (list, state) => _filterInMemoryDelegate(list, state)
            );
        }
        else if (_downloadPageDelegate != null)
        {
            Broker.ConfigureServerMode(_downloadPageDelegate);
        }
        else
        {
            throw new InvalidOperationException($"Контекст для сущности {typeof(TEntity).Name} не был сконфигурирован. Вызовите ConfigureUsingInMemoryMode или ConfigureUsingServerSideMode в конструкторе.");
        }

        // Вызов виртуального хука для подгрузки метаданных (например, справочников фильтров)
        await LoadMetadataInternalAsync();

        _isTransportActivated = true;
        NotifyContextUpdated();
    }

    /// <summary>
    /// Виртуальный хук ядра для ленивой асинхронной подгрузки метаданных фильтров.
    /// </summary>
    protected virtual Task LoadMetadataInternalAsync() => Task.CompletedTask;

    protected abstract TResultData GetEmptyDataState();

    /// <summary>
    /// ЕДИНАЯ ТОЧКА ЗАПРОСА ДАННЫХ ДЛЯ MUDBLAZOR.
    /// </summary>
    public virtual async Task<TResultData> GetDataAsync(TQueryState state, CancellationToken ct = default)
    {
        if (!_isTransportActivated)
            return GetEmptyDataState();

        if (_isLoading)
            return default!;

        try
        {
            _isLoading = true;
            return await Broker.FetchDataAsync(state, ct);
        }
        finally
        {
            _isLoading = false;
            NotifyContextUpdated(); // Кнопки тулбара мгновенно пересчитают IsEnabled на основе новых строк
        }
    }
}