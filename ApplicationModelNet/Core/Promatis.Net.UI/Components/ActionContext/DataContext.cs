using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактное инфраструктурное ядро работы с данными. 
/// Инкапсулирует работу брокера, кэша и состояния загрузки, общаясь с сервисами только по интерфейсу.
/// </summary>
public abstract class DataContext<TEntity, TKey, TQueryState, TResultData> : WorkspaceContext,
    IHasSelectedData<TEntity>,
    IDisposable
    where TEntity : class, new()
    where TKey : notnull
{
    private readonly IBaseService<TEntity, TKey> _baseService;
    private TEntity? _selectedData;
    private bool _isLoading;
    private bool _isDisposed;

    public IUiDataBroker<TEntity, TQueryState, TResultData> Broker { get; }
    public IUiOzuCache<TEntity> OzuCache { get; }

    public TEntity? SelectedData
    {
        get => _selectedData;
        set
        {
            if (_selectedData != value)
            {
                _selectedData = value;
                OnContextUpdated?.Invoke();
                NotifyStateChanged();
            }
        }
    }

    public Action? OnContextUpdated { get; set; }
    public override bool IsLoading => _isLoading;

    protected DataContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base()
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        _baseService = serviceProvider.GetRequiredService<IBaseService<TEntity, TKey>>();
        OzuCache = serviceProvider.GetRequiredService<IUiOzuCache<TEntity>>();
        Broker = serviceProvider.GetRequiredService<IUiDataBroker<TEntity, TQueryState, TResultData>>();

        // ИСПРАВЛЕНО: Возвращаем конфигурацию на её законное место. Брокер готов к работе с первой секунды!
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
    /// Единая и универсальная точка запроса данных для базового холста страницы.
    /// Включает визуальный индикатор загрузки (_isLoading) как для ServerMode, так и для InMemoryMode.
    /// </summary>
    public async Task<TResultData> GetDataAsync(TQueryState state, CancellationToken ct = default)
    {
        if (_isLoading) return default!;

        try
        {
            _isLoading = true;
            NotifyStateChanged(); // Показываем крутилку загрузки на экране

            return await Broker.FetchDataAsync(state, ct);
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged(); // Скрываем крутилку после отрисовки данных
        }
    }

    /// <summary>
    /// Служебный ленивый поставщик данных для Брокера (InMemory Mode).
    /// </summary>
    private async Task<List<TEntity>> LoadInitialDataForBrokerAsync(TQueryState state, CancellationToken ct)
    {
        try
        {
            return await _baseService.GetAllAsync(ct) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    protected abstract TResultData EvaluateDataStateInMemory(TQueryState state, IReadOnlyList<TEntity> inMemoryList);
    protected abstract Task<TResultData> FetchDataFromServerAsync(TQueryState state, CancellationToken ct);
    protected IBaseService<TEntity, TKey> GetBaseService() => _baseService;

    public virtual void Dispose()
    {
        if (_isDisposed) return;

        Broker?.Dispose();
        _isDisposed = true;
    }
}