using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактное инфраструктурное ядро работы с данными. 
/// Инкапсулирует работу брокера, кэша и состояния загрузки, общаясь с сервисами только по интерфейсу.
/// Полностью инвариантно к визуальным компонентам разметки (MudBlazor).
/// </summary>
public abstract class DataWorkspaceContext<TEntity, TKey, TQueryState, TResultData> : WorkspaceActionContext,
    IHasSelectedData<TEntity>,
    IDisposable
    where TEntity : class, new()
    where TKey : notnull
{
    private readonly IBaseService<TEntity, TKey> _baseService;
    private TEntity? _selectedData;
    private bool _isLoading;
    private bool _isDisposed;

    // Брокер и кэш оперируют абстрактными типами стейта и результата запроса
    public UiDataBroker<TEntity, TQueryState, TResultData> Broker { get; }
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
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Конструктор принимает IServiceProvider, ликвидируя ад конструкторов на нижних уровнях.
    /// </summary>
    protected DataWorkspaceContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Автоматически извлекаем базовый бизнес-сервис из DI-контейнера по типам сущности
        _baseService = serviceProvider.GetRequiredService<IBaseService<TEntity, TKey>>();

        OzuCache = new UiOzuCache<TEntity>();

        // Брокер инициализируется один раз, предотвращая утечки памяти при наследовании
        Broker = new UiDataBroker<TEntity, TQueryState, TResultData>(onDataChangedNotifier);

        if (isInMemoryMode)
        {
            Broker.ConfigureInMemoryMode(OzuCache, EvaluateDataStateInMemory);
        }
        else
        {
            Broker.ConfigureServerMode(FetchDataFromServerAsync);
        }
    }

    /// <summary>
    /// Первичная асинхронная загрузка данных с сервера (актуально для InMemory режима).
    /// </summary>
    public async Task LoadInitialDataAsync()
    {
        if (!Broker.IsInMemoryMode || _isLoading) return;

        try
        {
            _isLoading = true;
            NotifyStateChanged();

            List<TEntity> serverItems = await _baseService.GetAllAsync();
            OzuCache.InMemoryItems = serverItems ?? new List<TEntity>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Абстрактный метод локальной обработки стейта в оперативной памяти. 
    /// Специфику маппинга (например, пагинацию GridState) задаст конкретный контекст представления (Шаг 4).
    /// </summary>
    protected abstract TResultData EvaluateDataStateInMemory(TQueryState state, List<TEntity> inMemoryList);

    /// <summary>
    /// Абстрактный метод честного серверного запроса. 
    /// Специфику маппинга параметров на бэкэнд задаст конкретный контекст представления (Шаг 4).
    /// </summary>
    protected abstract Task<TResultData> FetchDataFromServerAsync(TQueryState state, CancellationToken ct);

    /// <summary>
    /// Внутренний доступ к базовому сервису для наследников (нужен для операций CRUD на Шаге 3).
    /// </summary>
    protected IBaseService<TEntity, TKey> GetBaseService() => _baseService;

    /// <summary>
    /// Гарантированная отписка брокера от статических событий триггеров БД при уничтожении контекста.
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed) return;

        Broker?.Dispose();
        _isDisposed = true;
    }
}