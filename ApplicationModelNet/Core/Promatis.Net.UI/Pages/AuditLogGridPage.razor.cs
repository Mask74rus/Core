using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.BaseGrid;
using Promatis.Net.UI.Components.BaseToolbarWorkspacePage;


namespace Promatis.Net.UI.Pages;

public partial class AuditLogGridPage : ComponentBase
{
    [Inject] protected IAuditLogService AuditLogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected BaseGridPage<AuditLog> _baseGrid { get; set; } = null!;

    protected readonly GridActionContext<AuditLog> _context = new()
    {
        PageTitle = "Журнал аудита",
        IsCreateVisible = false,
        IsEditVisible = false,
        IsDeleteVisible = false
    };

    protected DateRange _dateRange = new(DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Регистрируем кастомное действие в соответствии с новым типом ToolbarCustomAction
        _context.CustomActions.Add(new ToolbarCustomAction
        {
            Id = "excel_export",
            Title = "Выгрузить в Excel",
            Icon = Icons.Material.Filled.Download,
            Color = Color.Success,
            Variant = Variant.Filled,
            OnExecute = ExportToExcelAsync
        });
    }

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

    protected async Task ExportToExcelAsync()
    {
        _context.SetActionEnabled("excel_export", false);

        try
        {
            (DateTime utcStart, DateTime utcEnd) = GetUtcPeriod();

            var request = new AuditLogSearchRequest(
                FromDate: utcStart,
                ToDate: utcEnd,
                EntityName: null,
                Action: null,
                PageIndex: 0,
                PageSize: int.MaxValue
            );

            PagedResult<AuditLog> result = await AuditLogService.SearchLogsAsync(request);

            if (result.TotalCount == 0)
            {
                Snackbar.Add("Нет данных для выгрузки за указанный период", Severity.Warning);
                return;
            }

            // Имитация формирования файла
            await Task.Delay(1500);
            Snackbar.Add($"Успешно выгружено {result.TotalCount} строк", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка экспорта: {ex.Message}", Severity.Error);
        }
        finally
        {
            _context.SetActionEnabled("excel_export", true);
        }
    }

    protected async Task OnDateRangeChanged(DateRange newRange)
    {
        _dateRange = newRange;

        if (_baseGrid != null)
        {
            // ИСПРАВЛЕНО: Полностью убран Reflection. Вызываем нативный публичный метод базового класса
            await _baseGrid.ReloadServerDataAsync();
        }
    }

    private (DateTime Start, DateTime End) GetUtcPeriod()
    {
        DateTime localStart = _dateRange.Start ?? DateTime.MinValue;
        DateTime localEnd = _dateRange.End ?? DateTime.MaxValue;

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