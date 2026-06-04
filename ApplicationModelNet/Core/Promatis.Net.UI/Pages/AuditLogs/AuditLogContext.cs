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
    private readonly Action? _onFilterChanged; // ИСПРАВЛЕНО: Сделано nullable для DI-контейнера

    public override string TopZoneHeight => "48px";

    /// <summary>
    /// ИСПРАВЛЕНО: Параметр onFilterChanged сделан необязательным (= null).
    /// Это заставляет .NET DI успешно проходить валидацию дескрипторов при старте ядра платформы,
    /// но сохраняет возможность для страницы передать туда живой коллбек через оператор new!
    /// </summary>
    public AuditLogContext(IServiceProvider serviceProvider, Action? onFilterChanged = null)
        : base(serviceProvider, isInMemoryMode: false, onDataChangedNotifier: onFilterChanged)
    {
        _onFilterChanged = onFilterChanged;
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();

        AddControl(new AuditToolbarDivider());
        AddControl(new AuditExportButton());
    }

    public void InitializeFilters(List<string> entityNames)
    {
        // Защитная проверка: если контекст был создан без коллбека (в DI), фильтры будут использовать NotifyStateChanged
        Action callback = _onFilterChanged ?? NotifyStateChanged;

        _controls.Insert(0, new AuditEntitySelect(entityNames, callback));
        _controls.Insert(1, new AuditActionSelect(callback));
        _controls.Insert(2, new AuditPeriodPicker(callback));

        NotifyStateChanged();
    }

    protected override async Task<GridData<AuditLog>> FetchDataFromServerAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        // 1. Считываем элементы управления из коллекции по их жестким Id
        IUiControl? entityControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_entity");
        IUiControl? actionControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_action");
        IUiControl? periodControl = Controls.FirstOrDefault(c => c.Id == "audit_filter_period");

        string? selectedEntity = entityControl is IHasValue ev ? ev.Value as string : null;
        DateRange? selectedPeriod = periodControl is IHasValue pv ? pv.Value as DateRange : null;
        string? selectedAction = actionControl is AuditActionSelect av ? av.GetSelectedActionValue() : null;

        if (selectedEntity == "Все сущности")
            selectedEntity = null;

        // Выставляем временные границы (минимум за 7 дней, если период не выбран)
        DateTime fromDate = selectedPeriod?.Start ?? DateTime.Today.AddDays(-7);
        DateTime toDate = selectedPeriod?.End ?? DateTime.Today;

        fromDate = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        toDate = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        // 2. Строим специализированный DTO-запрос
        var searchRequest = new AuditLogSearchRequest(
            FromDate: fromDate,
            ToDate: toDate,
            EntityName: selectedEntity,
            Action: selectedAction,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        // 3. Запрашиваем постраничные данные из PostgreSQL
        PagedResult<AuditLog> pagedResult = await _auditLogService.SearchLogsAsync(searchRequest, ct);

        // ИСПРАВЛЕНО (Устранение ошибки типов): Приводим к универсальному IEnumerable, 
        // что полностью снимает конфликт между IReadOnlyCollection и List!
        IEnumerable<AuditLog> finalItems = pagedResult.Items;

        // 4. Если пользователь нажал на заголовок колонки — упорядочиваем полученные строки
        if (state.SortDefinitions.Any() && finalItems.Any())
        {
            finalItems = finalItems
                .AsQueryable()
                .OrderBy(state.SortDefinitions); // Нативный LINQ-разбор от MudBlazor
        }

        return new GridData<AuditLog>
        {
            // MudBlazor на входе в GridData принимает любое IEnumerable, поэтому каст не нужен!
            Items = finalItems,
            TotalItems = pagedResult.TotalCount
        };
    }

    protected override Task OpenDialogWindowAsync(AuditLog model, bool isNew)
    {
        return Task.CompletedTask;
    }
}