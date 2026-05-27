using MudBlazor;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.Grid;
using Promatis.Net.UI.Components.Toolbar;

namespace Promatis.Net.UI.Pages.AuditLog;

/// <summary>
/// Контекст управления прикладной страницей журнала аудита.
/// Выступает единым медиатором для холста, тулбара и таблицы в ОЗУ.
/// </summary>
/// <summary>
/// Контекст управления прикладной страницей журнала аудита.
/// Полностью полагается на автоматику обновлений ядра GridActionContext.
/// </summary>
public class AuditLogPageContext : GridActionContext<Domain.AuditLog>
{
    private readonly IAuditLogService _auditLogService;
    private DateRange _period = new(DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));

    /// <summary>
    /// Период логов, к которому напрямую привязан MudDateRangePicker в разметке.
    /// </summary>
    public DateRange Period
    {
        get => _period;
        set
        {
            if (_period != value)
            {
                _period = value;
                NotifyUpdate();     // Мгновенно перерисовываем тулбар (пикер дат)
                RequestRefresh();   // Пиняем GridPage на перезапрос серверных данных
            }
        }
    }

    // Автоматически внедряется .NET 10 DI рантаймом при открытии вкладки
    public AuditLogPageContext(IAuditLogService auditLogService) : base()
    {
        _auditLogService = auditLogService;

        PageTitle = "Журнал аудита";

        // Декларативно отключаем базовый CRUD
        IsCreateVisible = false;
        IsEditVisible = false;
        IsDeleteVisible = false;

        // Настраиваем наш универсальный Брокер Данных на серверный постраничный поиск
        DataBroker.ConfigureServerMode(FetchServerDataInternalAsync);

        // Безопасно добавляем кастомную кнопку через платформенный метод контроля уникальности
        AddCustomAction(new ToolbarCustomAction
        {
            Id = "excel_export",
            Title = "Выгрузить в Excel",
            Icon = Icons.Material.Filled.Download,
            Color = Color.Success,
            Variant = Variant.Filled,
            OnExecute = ExportToExcelInternalAsync
        });
    }

    private async Task<GridData<Domain.AuditLog>> FetchServerDataInternalAsync(GridState<Domain.AuditLog> state, CancellationToken ct)
    {
        DateTime localStart = Period.Start ?? DateTime.MinValue;
        DateTime localEnd = Period.End ?? DateTime.MaxValue;

        if (Period.End.HasValue && Period.End.Value.TimeOfDay == TimeSpan.Zero)
        {
            localEnd = Period.End.Value.AddDays(1).AddSeconds(-1);
        }

        var request = new AuditLogSearchRequest(
            FromDate: localStart.ToUniversalTime(),
            ToDate: localEnd.ToUniversalTime(),
            EntityName: null,
            Action: null,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        PagedResult<Domain.AuditLog> result = await _auditLogService.SearchLogsAsync(request, ct);

        return new GridData<Domain.AuditLog>
        {
            TotalItems = result.TotalCount,
            Items = result.Items
        };
    }

    private async Task ExportToExcelInternalAsync()
    {
        SetActionEnabled("excel_export", false);
        try
        {
            await Task.Delay(1000);
        }
        finally
        {
            SetActionEnabled("excel_export", true);
        }
    }
}