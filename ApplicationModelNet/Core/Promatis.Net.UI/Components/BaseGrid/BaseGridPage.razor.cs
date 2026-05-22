using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.BaseGrid;

public partial class BaseGridPage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    [Parameter] public IEnumerable<TEntity>? Items { get; set; }
    [Parameter] public Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? ServerData { get; set; }
    [Parameter] public bool IsLoading { get; set; } = false;
    [Parameter] public RenderFragment? ColumnsContent { get; set; }
    [Parameter] public RenderFragment? PagerContent { get; set; }
    [Parameter] public RenderFragment? AdditionalToolbarContent { get; set; }
    [Parameter] public GridActionContext<TEntity> ActionContext { get; set; } = null!;

    [Parameter] public EventCallback OnCreateTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteTriggered { get; set; }

    // НОВЫЙ ТРИГГЕР: Вызывается, если таблица работает в режиме плоского списка Items,
    // чтобы заставить бизнес-страницу перечитать данные из базы.
    [Parameter] public EventCallback OnDataChangedRefreshRequested { get; set; }

    private MudDataGrid<TEntity> _grid = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ActionContext ??= new GridActionContext<TEntity>();

        // Синхронизируем обновление UI при изменении контекста таблицы
        ActionContext.OnContextUpdated = StateHasChanged;

        // РЕАКТИВНАЯ АВТОМАТИКА: Подписываемся на импульсы обновлений из контекста холста
        ActionContext.OnRefreshRequested += HandleRefreshRequest;
    }

    /// <summary>
    /// Ловит сигнал из контекста и запускает молниеносное обновление данных на экране
    /// </summary>
    private void HandleRefreshRequest()
    {
        InvokeAsync(async () =>
        {
            if (ServerData != null)
            {
                // Если грид на ServerData — пинаем нативный метод перезагрузки MudBlazor
                await ReloadServerDataAsync();
            }
            else if (OnDataChangedRefreshRequested.HasDelegate)
            {
                // Если грид на плоской коллекции — просим страницу обновить коллекцию Items
                await OnDataChangedRefreshRequested.InvokeAsync();
            }

            StateHasChanged();
        });
    }

    public Task ReloadServerDataAsync() => _grid != null ? _grid.ReloadServerData() : Task.CompletedTask;

    protected async Task OnCreateClick()
    {
        if (OnCreateTriggered.HasDelegate) await OnCreateTriggered.InvokeAsync();
    }

    protected async Task OnEditClick()
    {
        if (OnEditTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnEditTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnDeleteClick()
    {
        if (OnDeleteTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnDeleteTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    private void OnSelectedItemChanged(TEntity? newItem)
    {
        if (newItem == null && ActionContext.SelectedData != null)
        {
            return;
        }

        ActionContext.SelectedData = newItem;
    }

    /// <summary>
    /// Уничтожает подписку при закрытии вкладки MDI, исключая утечки ОЗУ
    /// </summary>
    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnRefreshRequested -= HandleRefreshRequest;
        }
    }
}