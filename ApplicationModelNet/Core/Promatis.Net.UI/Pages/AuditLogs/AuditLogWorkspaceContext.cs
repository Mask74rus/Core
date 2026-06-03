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
public class AuditLogWorkspaceContext : GridWorkspaceContext<AuditLog, Guid>
{
    private readonly IAuditLogService _auditLogService;
    private readonly Action _onFilterChanged;

    public override string TopZoneHeight => "48px";

    /// <summary>
    /// Конструктор принимает только единственный IServiceProvider и коллбек обновления.
    /// Бизнес-сервис IAuditLogService извлекается автоматически (через явный каст базового сервиса ядра).
    /// </summary>
    public AuditLogWorkspaceContext(IServiceProvider serviceProvider, Action onFilterChanged)
        : base(serviceProvider, isInMemoryMode: false, onDataChangedNotifier: onFilterChanged)
    {
        _onFilterChanged = onFilterChanged ?? throw new ArgumentNullException(nameof(onFilterChanged));

        // Безопасно извлекаем специфичный интерфейс службы аудита из DI-контейнера
        _auditLogService = (IAuditLogService)GetBaseService();

        // Сразу наполняем тулбар статическими кнопками с правым выравниванием
        AddControl(new AuditToolbarDivider());
        AddControl(new AuditExportButton());
    }

    /// <summary>
    /// Динамическая дособирка тулбара асинхронно полученными данными из БД.
    /// Вызывается из OnAfterRenderAsync Razor-страницы логов.
    /// </summary>
    public void InitializeFilters(List<string> entityNames)
    {
        // Вставляем фильтры в самое начало тулбара (слева направо)
        _controls.Insert(0, new AuditEntitySelect(entityNames, _onFilterChanged));
        _controls.Insert(1, new AuditActionSelect(_onFilterChanged));
        _controls.Insert(2, new AuditPeriodPicker(_onFilterChanged));

        NotifyStateChanged();
    }

    /// <summary>
    /// Переопределенная специфичная серверная пагинация и фильтрация логов аудита.
    /// Идеально мапит табличный стейт MudBlazor в сложный DTO-запрос к PostgreSQL.
    /// </summary>
    protected override async Task<GridData<AuditLog>> FetchDataFromServerAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        // Извлекаем элементы управления из коллекции по их жестким Id
        IUiControl? entityControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_entity");
        IUiControl? actionControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_action");
        IUiControl? periodControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_period");

        // Безопасно извлекаем значения через интерфейс-маркер IHasValue
        string? selectedEntity = entityControl is IHasValue ev ? ev.Value as string : null;
        DateRange? selectedPeriod = periodControl is IHasValue pv ? pv.Value as DateRange : null;

        // Извлекаем системное имя операции (Added, Modified, Deleted, SoftDeleted)
        string? selectedAction = actionControl is AuditActionSelect av ? av.GetSelectedActionValue() : null;

        if (selectedEntity == "Все сущности")
            selectedEntity = null;

        // Выставляем временные границы (минимум за 7 дней, если период не выбран)
        DateTime fromDate = selectedPeriod?.Start ?? DateTime.Today.AddDays(-7);
        DateTime toDate = selectedPeriod?.End ?? DateTime.Today;

        fromDate = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        toDate = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        // Строим специализированный DTO-запрос
        var searchRequest = new AuditLogSearchRequest(
            FromDate: fromDate,
            ToDate: toDate,
            EntityName: selectedEntity,
            Action: selectedAction,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        // Запрашиваем данные через специфичный метод интерфейса IAuditLogService
        PagedResult<AuditLog> pagedResult = await _auditLogService.SearchLogsAsync(searchRequest, ct);

        return new GridData<AuditLog>
        {
            Items = pagedResult.Items,
            TotalItems = pagedResult.TotalCount
        };
    }

    /// <summary>
    /// Заглушка абстрактного метода ядра. Журнал логов работает только для чтения, 
    /// поэтому модальные окна здесь не используются.
    /// </summary>
    protected override Task OpenDialogWindowAsync(AuditLog model, bool isNew)
    {
        return Task.CompletedTask;
    }
}