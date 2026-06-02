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

    public UiDataBroker<TEntity, GridState<TEntity>, GridData<TEntity>> Broker { get; protected set; }
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

    protected CrudWorkspaceContext(IBaseService<TEntity, TKey> baseService, bool isInMemoryMode)
    {
        _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
        OzuCache = new UiOzuCache<TEntity>();
        Broker = new UiDataBroker<TEntity, GridState<TEntity>, GridData<TEntity>>();

        // Инициализируем элементы управления через виртуальный метод
        InitializeToolbarControls();

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
    /// Наполнение тулбара кнопками по умолчанию. Наследники могут подавить или изменить состав кнопок.
    /// </summary>
    protected virtual void InitializeToolbarControls()
    {
        AddControl(new CreateEntityButton<TEntity>());
        AddControl(new EditEntityButton<TEntity>());
        AddControl(new DeleteEntityButton<TEntity>());
        AddControl(new ToolbarDivider());
    }

    public async Task LoadInitialDataAsync()
    {
        if (!Broker.IsInMemoryMode) return;

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

    protected GridData<TEntity> EvaluateGridStateInMemory(GridState<TEntity> state, List<TEntity> inMemoryList)
    {
        return new GridData<TEntity>
        {
            Items = inMemoryList.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList(),
            TotalItems = inMemoryList.Count
        };
    }

    private async Task<GridData<TEntity>> FetchGridDataFromServerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        // Теперь здесь работает честная серверная пагинация без утечек памяти
        PagedResult<TEntity> pagedResult = await _baseService.GetPagedAsync(state.Page, state.PageSize, ct);

        return new GridData<TEntity>
        {
            Items = pagedResult.Items,
            TotalItems = pagedResult.TotalCount
        };
    }

}