using MudBlazor;
using Promatis.Net.Service;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Обобщенный базовый контекст для стандартных CRUD-экранов платформы.
/// Полностью автоматизирует фоновую загрузку данных на базе IBaseService.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности (должен быть классом с конструктором по умолчанию).</typeparam>
/// <typeparam name="TKey">Тип первичного ключа сущности (Guid, int, string и т.д.).</typeparam>
public abstract class CrudWorkspaceContext<TEntity, TKey> : WorkspaceActionContext, IHasSelectedData<TEntity>
    where TEntity : class, new()
    where TKey : notnull
{
    private readonly IBaseService<TEntity, TKey> _baseService;
    private TEntity? _selectedData;
    private bool _isLoading;

    // Инфраструктурные компоненты данных инстанса формы
    public UiDataBroker<TEntity, GridState<TEntity>, GridData<TEntity>> Broker { get; }
    public IUiOzuCache<TEntity> OzuCache { get; }

    // Реализация контракта фокуса строки
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

    /// <summary>
    /// Флаг фоновой загрузки для отображения скелетона или крутилки в GridPage.
    /// </summary>
    public bool IsLoading => _isLoading;

    protected CrudWorkspaceContext(IBaseService<TEntity, TKey> baseService, bool isInMemoryMode)
    {
        _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
        OzuCache = new UiOzuCache<TEntity>();
        Broker = new UiDataBroker<TEntity, GridState<TEntity>, GridData<TEntity>>();

        // Декларативно наполняем тулбар стандартными CRUD-кнопками ядра
        AddControl(new CreateEntityButton<TEntity>());
        AddControl(new EditEntityButton<TEntity>());
        AddControl(new DeleteEntityButton<TEntity>());
        AddControl(new ToolbarDivider());

        // НАСТРОЙКА ПОВЕДЕНИЯ: Выбор источника данных на основе решения программиста
        if (isInMemoryMode)
        {
            Broker.ConfigureInMemoryMode(OzuCache, EvaluateGridStateInMemory);
        }
        else
        {
            Broker.ConfigureServerMode(FetchGridDataFromServerAsync);
        }
    }

    /// <summary>
    /// Точка ленивой, безопасной фоновой загрузки первичных данных.
    /// Вызывается страницей при инициализации и гарантированно не блокирует UI Blazor.
    /// </summary>
    public async Task LoadInitialDataAsync()
    {
        if (!Broker.IsInMemoryMode) return; // В серверном режиме фоновое наполнение кэша не требуется

        try
        {
            _isLoading = true;
            NotifyStateChanged(); // Включаем индикатор загрузки в таблице

            // Выкачиваем весь справочник через ваш реальный метод GetAllAsync()
            List<TEntity> serverItems = await _baseService.GetAllAsync();
            OzuCache.InMemoryItems = serverItems ?? new List<TEntity>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged(); // Выключаем индикатор, таблица плавно отображает строки из ОЗУ
        }
    }

    // --- СТАНДАРТНЫЕ МЕТОДЫ-ВЫЧИСЛИТЕЛИ ДЛЯ БРОКЕРА ---

    private GridData<TEntity> EvaluateGridStateInMemory(GridState<TEntity> state, List<TEntity> inMemoryList)
    {
        // Пагинация плоского списка LINQ методами в памяти ОЗУ конкретной вкладки
        return new GridData<TEntity>
        {
            Items = inMemoryList.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList(),
            TotalItems = inMemoryList.Count
        };
    }

    private async Task<GridData<TEntity>> FetchGridDataFromServerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        // Прямой серверный режим: при каждом запросе идем в СУБД (например, для тяжелых логов)
        List<TEntity> serverData = await _baseService.GetAllAsync();
        return new GridData<TEntity>
        {
            Items = serverData.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList(),
            TotalItems = serverData.Count
        };
    }
}