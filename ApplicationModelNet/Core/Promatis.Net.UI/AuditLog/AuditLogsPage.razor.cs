using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.AuditLog; // Укажите здесь ваше реальное пространство имен (namespace) для папки с компонентом

public partial class AuditLogsPage : BaseDataGrid<Domain.AuditLog>
{
    [Inject]
    protected IAuditLogService AuditLogService { get; set; } = null!;

    // В MudBlazor v9.4 DateRange иммутабелен, инициализируем через конструктор
    protected DateRange LogDateRange { get; set; } = new(DateTime.Today.AddDays(-7), DateTime.Today);

    protected string? SelectedEntity;
    protected string? SelectedAction;
    protected List<string> AvailableEntities = new();

    // Сигнатура метода-моста теперь строго соответствует требованиям MudDataGrid ServerData делегата
    protected async Task<GridData<GridRowModel<Domain.AuditLog>>> LoadGridDataAsyncInternal(
        GridState<GridRowModel<Domain.AuditLog>> state,
        CancellationToken token)
    {
        // Передаем токен, сгенерированный компонентом MudDataGrid, дальше в базовый метод
        return await LoadGridDataAsync(state, token);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Загружаем фильтры сущностей, когда UI уже отзывчив (Мгновенное открытие страниц)
                // Используем долгоживущий токен Cts.Token из базового класса BaseDataGrid
                AvailableEntities = await AuditLogService.GetAvailableEntityNamesAsync(Cts.Token);

                // Принудительно уведомляем Blazor, что данные фильтра обновились
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Не удалось загрузить фильтры сущностей: {ex.Message}", Severity.Error);
            }
        }
    }

    protected override async Task<GridData<GridRowModel<Domain.AuditLog>>> LoadGridDataAsync(
        GridState<GridRowModel<Domain.AuditLog>> state,
        CancellationToken token)
    {
        if (LogDateRange.Start == null || LogDateRange.End == null)
        {
            return new GridData<GridRowModel<Domain.AuditLog>> { Items = Array.Empty<GridRowModel<Domain.AuditLog>>(), TotalItems = 0 };
        }

        IsLoading = true;
        StateHasChanged();

        try
        {
            // 1. Берем чистые даты начала и конца дня в локальном времени
            var localFrom = LogDateRange.Start.Value.Date;
            var localTo = LogDateRange.End.Value.Date.AddDays(1).AddTicks(-1);

            // 2. Принудительно конвертируем их в UTC перед отправкой в сервис и PostgreSQL
            var utcFrom = DateTime.SpecifyKind(localFrom, DateTimeKind.Local).ToUniversalTime();
            var utcTo = DateTime.SpecifyKind(localTo, DateTimeKind.Local).ToUniversalTime();

            var request = new AuditLogSearchRequest(
                FromDate: utcFrom,
                ToDate: utcTo,
                EntityName: SelectedEntity,
                Action: SelectedAction,
                PageIndex: state.Page,
                PageSize: state.PageSize
            );

            // Запрашиваем чистый список доменных объектов из сервиса, прокидывая токен отмены
            var result = await AuditLogService.SearchLogsAsync(request, token);

            // Оборачиваем доменные объекты в UI-модели на лету перед рендерингом (Околонулевой маппинг)
            var mappedItems = result.Items.Select(x => new GridRowModel<Domain.AuditLog>(x)).ToList();

            return new GridData<GridRowModel<Domain.AuditLog>>
            {
                Items = mappedItems,
                TotalItems = result.TotalCount
            };
        }
        catch (OperationCanceledException)
        {
            // Корректно обрабатываем отмену операции (например, при быстром переклике пагинации)
            return new GridData<GridRowModel<Domain.AuditLog>> { Items = Array.Empty<GridRowModel<Domain.AuditLog>>(), TotalItems = 0 };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка при получении логов: {ex.Message}", Severity.Error);
            return new GridData<GridRowModel<Domain.AuditLog>> { Items = Array.Empty<GridRowModel<Domain.AuditLog>>(), TotalItems = 0 };
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected Task<IEnumerable<string>> SearchEntitiesAsync(string? value, CancellationToken token)
    {
        if (string.IsNullOrEmpty(value))
            return Task.FromResult<IEnumerable<string>>(AvailableEntities);

        IEnumerable<string> filtered = AvailableEntities
            .Where(x => x.Contains(value, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(filtered);
    }

    protected async Task OnDateRangeChanged(DateRange newRange)
    {
        LogDateRange = newRange;
        await Grid.ReloadServerData();
    }

    protected async Task OnEntityChanged(string? entity)
    {
        SelectedEntity = entity;
        await Grid.ReloadServerData();
    }

    protected async Task OnActionChanged(string? action)
    {
        SelectedAction = action;
        await Grid.ReloadServerData();
    }
}