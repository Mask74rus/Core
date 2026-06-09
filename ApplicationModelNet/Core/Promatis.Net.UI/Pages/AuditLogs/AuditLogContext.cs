using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Pages.AuditLogs.Toolbar;

namespace Promatis.Net.UI.Pages.AuditLogs;

/// <summary>
/// Контекст страницы логирования аудита. 
/// Фокусируется исключительно на логике фильтрации и маппинге параметров поиска.
/// </summary>

public class AuditLogContext : GridContext<AuditLog, Guid>, IToolbarContext
{
    private readonly IAuditLogService _auditLogService;

    // СТРОГО ТИПИЗИРОВАННЫЕ ФИЛЬТРЫ ВНУТРИ КОНТЕКСТА
    public AuditActionSelect ActionFilter { get; }
    public AuditPeriodPicker PeriodFilter { get; }
    public AuditEntitySelect EntityFilter { get; }

    // ИЗОЛИРОВАННЫЙ СИГНАЛ ДЛЯ СТРАНИЦЫ: Вызывается строго при мутации фильтров
    public event Action? OnFiltersChanged;

    public Lock ControlsLock { get; } = new();
    public List<IUiControl> InnerControls { get; } = [];
    public bool IsToolbarInitialized { get; set; }

    public override string TopZoneHeight => "48px";

    public AuditLogContext(IServiceProvider serviceProvider)
        : base(serviceProvider, isInMemoryMode: false)
    {
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();

        // Фильтры при изменении дергают внутренний метод-распределитель
        ActionFilter = new AuditActionSelect(HandleFilterMutation);
        PeriodFilter = new AuditPeriodPicker(HandleFilterMutation);
        EntityFilter = new AuditEntitySelect(new List<string>(), HandleFilterMutation);
    }

    /// <summary>
    /// ВНУТРЕННИЙ ДИСПЕТЧЕР КНОПОК: Вызывается комбобоксами тулбара.
    /// </summary>
    private void HandleFilterMutation()
    {
        // 1. Пинаем тулбар на перерисовку
        NotifyContextUpdated();

        // 2. Пинаем страницу, чтобы она адресно обновила строки грида!
        OnFiltersChanged?.Invoke();
    }

    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        controls.Add(EntityFilter);
        controls.Add(ActionFilter);
        controls.Add(PeriodFilter);
        controls.Add(new AuditToolbarDivider());
        controls.Add(new AuditExportButton());
    }

    /// <summary>
    /// ФАЗА А: Наполнение выпадающего меню комбобокса. 
    /// Работает параллельно, не блокируя и не влияя на стартовую загрузку строк таблицы.
    /// </summary>
    protected override async Task LoadMetadataInternalAsync()
    {
        List<string> availableEntities = await _auditLogService.GetAvailableEntityNamesAsync();

        var list = new List<string> { "Все сущности" };
        list.AddRange(availableEntities);

        // Просто отдали опции в UI. Никаких импульсов перезагрузки грида отсюда слать НЕЛЬЗЯ!
        EntityFilter.Options = list;
    }

    /// <summary>
    /// ФАЗА Б: Серверный транспорт данных таблицы.
    /// Читает строго ТЕКУЩИЕ выбранные значения (Value) из объектов ОЗУ.
    /// При старте здесь гарантированно лежат дефолты, поэтому запрос выполнится мгновенно и без гонок.
    /// </summary>
    protected override async Task<GridData<AuditLog>> FetchDataFromServerAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        // Читаем выбранные пользователем критерии (Value)
        string? selectedAction = ActionFilter.GetSelectedActionValue();
        DateRange? selectedPeriod = PeriodFilter.Value as DateRange;
        string? selectedEntity = EntityFilter.Value as string; // При старте тут лежит "Все сущности"

        if (selectedEntity == "Все сущности") selectedEntity = null;

        DateTime fromDate = selectedPeriod?.Start ?? DateTime.Today.AddDays(-7);
        DateTime toDate = selectedPeriod?.End ?? DateTime.Today;

        fromDate = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        toDate = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var searchRequest = new AuditLogSearchRequest(
            FromDate: fromDate,
            ToDate: toDate,
            EntityName: selectedEntity,
            Action: selectedAction,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        PagedResult<AuditLog> pagedResult = await _auditLogService.SearchLogsAsync(searchRequest, ct);

        return new GridData<AuditLog>
        {
            Items = pagedResult.Items ?? new List<AuditLog>(),
            TotalItems = pagedResult.TotalCount
        };
    }

    protected override Task<List<AuditLog>> LoadInitialDataForBrokerAsync(GridState<AuditLog> state, CancellationToken ct)
        => Task.FromResult(new List<AuditLog>());
}