using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Controls;
using Promatis.Net.UI.Pages.AuditLogs.Toolbar;

namespace Promatis.Net.UI.Pages.AuditLogs;

public class AuditLogWorkspaceContext : WorkspaceActionContext, IHasSelectedData<AuditLog>
{
    private readonly IAuditLogService _auditLogService;
    private readonly Action _onFilterChanged;

    public UiDataBroker<AuditLog, GridState<AuditLog>, GridData<AuditLog>> Broker { get; }

    public AuditLog? SelectedData
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnContextUpdated?.Invoke();
                NotifyStateChanged();
            }
        }
    }

    public Action? OnContextUpdated { get; set; }

    public override string TopZoneHeight => "48px";

    public AuditLogWorkspaceContext(IAuditLogService auditLogService, Action onFilterChanged)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _onFilterChanged = onFilterChanged;

        Broker = new UiDataBroker<AuditLog, GridState<AuditLog>, GridData<AuditLog>>();
        Broker.ConfigureServerMode(FetchAuditDataFromServerAsync);

        // Заменяем new ToolbarDivider() на новый специализированный правый разделитель
        AddControl(new AuditToolbarDivider());
        AddControl(new AuditExportButton());
    }

    public void InitializeFilters(List<string> entityNames)
    {
        _controls.Insert(0, new AuditEntitySelect(entityNames, _onFilterChanged));
        _controls.Insert(1, new AuditActionSelect(_onFilterChanged));
        _controls.Insert(2, new AuditPeriodPicker(_onFilterChanged));
        NotifyStateChanged();
    }

    /// <summary>
    /// Серверная стратегия. Мапит состояние MudBlazor в строго позиционный конструктор AuditLogSearchRequest.
    /// </summary>
    private async Task<GridData<AuditLog>> FetchAuditDataFromServerAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        IUiControl? entityControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_entity");
        IUiControl? actionControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_action");
        IUiControl? periodControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_period");

        string? selectedEntity = entityControl is IHasValue ev ? ev.Value as string : null;
        DateRange? selectedPeriod = periodControl is IHasValue pv ? pv.Value as DateRange : null;

        // Получаем системное имя операции (create/update/delete или null)
        string? selectedAction = actionControl is AuditActionSelect av ? av.GetSelectedActionValue() : null;

        if (selectedEntity == "Все сущности") // Строка-заглушка из вашего кода
            selectedEntity = null;

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
            Items = pagedResult.Items,
            TotalItems = pagedResult.TotalCount
        };
    }
}