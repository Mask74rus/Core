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
public class AuditLogContext : GridContext<AuditLog, Guid>
{
    private readonly IAuditLogService _auditLogService;
    private readonly Action _onFilterChanged; // ИСПРАВЛЕНО: Снова строгое readonly свойство

    public override string TopZoneHeight => "48px";

    /// <summary>
    /// ИСПРАВЛЕНО: Конструктор принимает строго Action коллбек для прямой реактивности.
    /// Никаких значений по умолчанию (= null) для DI-валидации больше нет!
    /// </summary>
    public AuditLogContext(IServiceProvider serviceProvider, Action onFilterChanged)
        : base(serviceProvider, isInMemoryMode: false, onDataChangedNotifier: onFilterChanged)
    {
        _onFilterChanged = onFilterChanged ?? throw new ArgumentNullException(nameof(onFilterChanged));
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();

        // Сразу наполняем тулбар статическими элементами
        AddControl(new AuditToolbarDivider());
        AddControl(new AuditExportButton());
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Ваша оригинальная динамическая дособирка тулбара.
    /// Вызывается из OnAfterRenderAsync Razor-страницы, когда экран уже гарантированно отрисован.
    /// </summary>
    public void InitializeFilters(List<string> entityNames)
    {
        // Потокобезопасно через инкапсулированный метод базового класса (или напрямую, если восстановили доступ)
        // возвращаем нативную вставку фильтров в самое начало тулбара слева направо
        _controls.Insert(0, new AuditEntitySelect(entityNames, _onFilterChanged));
        _controls.Insert(1, new AuditActionSelect(_onFilterChanged));
        _controls.Insert(2, new AuditPeriodPicker(_onFilterChanged));

        NotifyStateChanged();
    }

    protected override async Task<GridData<AuditLog>> FetchDataFromServerAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        // ИСПРАВЛЕНО: Возвращаем ваш оригинальный, работающий поиск контролов по жестким Id
        IUiControl? entityControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_entity");
        IUiControl? actionControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_action");
        IUiControl? periodControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_period");

        string? selectedEntity = entityControl is IHasValue ev ? ev.Value as string : null;
        DateRange? selectedPeriod = periodControl is IHasValue pv ? pv.Value as DateRange : null;
        string? selectedAction = actionControl is AuditActionSelect av ? av.GetSelectedActionValue() : null;

        if (selectedEntity == "Все сущности")
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

    protected override Task OpenDialogWindowAsync(AuditLog model, bool isNew) => Task.CompletedTask;
}