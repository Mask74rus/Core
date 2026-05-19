using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Pages;

public partial class AuditLogGridPage : ComponentBase
{
    [Inject]
    protected IAuditLogService AuditLogService { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    private MudDataGrid<AuditLog> _grid = null!;

    // Владелец контекста — текущая страница. Объект создается один раз и жестко держит конфигурацию кнопок.
    protected readonly GridActionContext _context = new()
    {
        PageTitle = "Журнал аудита",
        IsCreateVisible = false,
        IsDeleteVisible = false,
        IsCreateEnabled = false,
        IsDeleteEnabled = false
    };

    // Локальный диапазон дат для корректного отображения в календаре пользователя
    protected DateRange _dateRange = new(DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));
    protected bool _isExporting;

    /// <summary>
    /// Серверный запрос пагинации для MudDataGrid (совместим с делегатом MudBlazor 9+)
    /// </summary>
    protected async Task<GridData<AuditLog>> LoadServerData(GridState<AuditLog> state, CancellationToken cancellationToken)
    {
        (DateTime utcStart, DateTime utcEnd) = GetUtcPeriod();

        var request = new AuditLogSearchRequest(
            FromDate: utcStart,
            ToDate: utcEnd,
            EntityName: null,
            Action: null,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        PagedResult<AuditLog> result = await AuditLogService.SearchLogsAsync(request, cancellationToken);

        return new GridData<AuditLog>
        {
            TotalItems = result.TotalCount,
            Items = result.Items
        };
    }

    /// <summary>
    /// Логика выгрузки данных логов в Excel
    /// </summary>
    protected async Task ExportToExcelAsync()
    {
        if (_isExporting) return;

        try
        {
            _isExporting = true;
            (DateTime utcStart, DateTime utcEnd) = GetUtcPeriod();

            var request = new AuditLogSearchRequest(
                FromDate: utcStart,
                ToDate: utcEnd,
                EntityName: null,
                Action: null,
                PageIndex: 0,
                PageSize: int.MaxValue // Выгружаем весь срез данных за выбранный день
            );

            PagedResult<AuditLog> result = await AuditLogService.SearchLogsAsync(request);

            if (result.TotalCount == 0)
            {
                Snackbar.Add("Нет данных для выгрузки за указанный период", Severity.Warning);
                return;
            }

            // ИМИТАЦИЯ ГЕНЕРАЦИИ EXCEL И СКАЧИВАНИЯ ЧЕРЕЗ JS:
            // var fileBytes = ExcelGenerator.Generate(result.Items);
            // await BlazorDownloadFileService.DownloadFile("audit_logs.xlsx", fileBytes, "application/vnd.ms-excel");
            await Task.Delay(1500);

            Snackbar.Add($"Успешно выгружено {result.TotalCount} строк", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка экспорта: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isExporting = false;
        }
    }

    /// <summary>
    /// Реактивный обработчик изменения дат в MudDateRangePicker
    /// </summary>
    protected async Task OnDateRangeChanged(DateRange newRange)
    {
        _dateRange = newRange;
        if (_grid != null)
        {
            await _grid.ReloadServerData();
        }
    }

    /// <summary>
    /// Принудительная конвертация локального периода в UTC стандарт со строгим Kind для драйвера PostgreSQL
    /// </summary>
    private (DateTime Start, DateTime End) GetUtcPeriod()
    {
        DateTime localStart = _dateRange.Start ?? DateTime.MinValue;
        DateTime localEnd = _dateRange.End ?? DateTime.MaxValue;

        // Если дата окончания выбрана без указания времени, расширяем её до конца локальных суток (23:59:59)
        if (_dateRange.End.HasValue && _dateRange.End.Value.TimeOfDay == TimeSpan.Zero)
        {
            localEnd = _dateRange.End.Value.AddDays(1).AddSeconds(-1);
        }

        DateTime utcStart = localStart.Kind == DateTimeKind.Utc
            ? localStart
            : DateTime.SpecifyKind(localStart.ToUniversalTime(), DateTimeKind.Utc);

        DateTime utcEnd = localEnd.Kind == DateTimeKind.Utc
            ? localEnd
            : DateTime.SpecifyKind(localEnd.ToUniversalTime(), DateTimeKind.Utc);

        return (utcStart, utcEnd);
    }

    protected Color GetActionColor(string action) => action.ToLower() switch
    {
        "create" or "insert" or "добавление" => Color.Success,
        "update" or "edit" or "изменение" => Color.Warning,
        "delete" or "remove" or "удаление" => Color.Error,
        _ => Color.Default
    };
}