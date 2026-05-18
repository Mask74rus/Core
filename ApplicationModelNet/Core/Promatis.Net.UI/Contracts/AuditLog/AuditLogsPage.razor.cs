using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Contracts.AuditLog; // Укажите здесь ваше реальное пространство имен (namespace) для папки с компонентом

public partial class AuditLogsPage : ComponentBase
{
    // Вместо @inject используем атрибут [Inject]
    [Inject] protected IAuditLogService AuditLogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected MudDataGrid<Domain.AuditLog> Grid { get; set; } = null!;
    protected bool IsLoading;

    // В MudBlazor v9.4 DateRange иммутабелен, инициализируем через конструктор
    protected DateRange LogDateRange { get; set; } = new(DateTime.Today.AddDays(-7), DateTime.Today);

    protected string? SelectedEntity;
    protected string? SelectedAction;
    protected List<string> AvailableEntities = new();

    // 1. Оставляем OnInitializedAsync абсолютно ПУСТЫМ или удаляем его.
    // Это гарантирует, что при клике на меню страница откроется МГНОВЕННО.
    protected override Task OnInitializedAsync()
    {
        return Task.CompletedTask;
    }

    // 2. Используем OnAfterRenderAsync для тяжелых фоновых запросов
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Код выполнится ТОЛЬКО один раз, сразу ПОСЛЕ того, как пользователь 
        // уже увидел форму логов на своем экране
        if (firstRender)
        {
            try
            {
                // Загружаем фильтры сущностей, когда UI уже отзывчив
                AvailableEntities = await AuditLogService.GetAvailableEntityNamesAsync();

                // Принудительно уведомляем Blazor, что данные фильтра обновились
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Не удалось загрузить фильтры сущностей: {ex.Message}", Severity.Error);
            }
        }
    }

    protected async Task<GridData<Domain.AuditLog>> LoadGridDataAsync(GridState<Domain.AuditLog> state, CancellationToken token)
    {
        if (LogDateRange.Start == null || LogDateRange.End == null)
        {
            return new GridData<Domain.AuditLog> { Items = Array.Empty<Domain.AuditLog>(), TotalItems = 0 };
        }

        IsLoading = true;
        StateHasChanged();

        try
        {
            // 1. Берем чистые даты начала и конца дня в локальном времени
            var localFrom = LogDateRange.Start.Value.Date;
            var localTo = LogDateRange.End.Value.Date.AddDays(1).AddTicks(-1);

            // 2. ИСПРАВЛЕНИЕ: Принудительно конвертируем их в UTC перед отправкой в сервис/БД
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

            var result = await AuditLogService.SearchLogsAsync(request, token);

            return new GridData<Domain.AuditLog>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (OperationCanceledException)
        {
            return new GridData<Domain.AuditLog> { Items = Array.Empty<Domain.AuditLog>(), TotalItems = 0 };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка при получении логов: {ex.Message}", Severity.Error);
            return new GridData<Domain.AuditLog> { Items = Array.Empty<Domain.AuditLog>(), TotalItems = 0 };
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