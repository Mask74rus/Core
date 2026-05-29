using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Grid;

public partial class GridPage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    [Parameter] public RenderFragment? ColumnsContent { get; set; }
    [Parameter] public RenderFragment? PagerContent { get; set; }

    [CascadingParameter] protected GridActionContext<TEntity> ActionContext { get; set; } = null!;

    [Inject]
    public IServiceProvider SystemServiceProvider { get; set; } = null!;

    private MudDataGrid<TEntity> _grid = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext == null)
        {
            throw new ArgumentNullException(nameof(ActionContext),
                $"Компонент {nameof(GridPage<TEntity>)} требует наличия {nameof(GridActionContext<TEntity>)} в каскадных параметрах.");
        }
        // Магия привязки: отдаем контексту страницы провайдер текущей живой сессии Blazor
        ActionContext.ScopedProvider = SystemServiceProvider;

        ActionContext.OnContextUpdated += StateHasChanged;
        ActionContext.OnRefreshRequested += HandleRefreshRequested;
    }

    private void HandleRefreshRequested()
    {
        InvokeAsync(ReloadServerDataAsync);
    }

    /// <summary>
    /// Внутренний метод-мост. Автоматически и безопасно вызывается компонентом MudDataGrid 
    /// в асинхронном контексте Blazor, исключая любые фризы меню.
    /// </summary>
    protected async Task<GridData<TEntity>> LoadGridDataInternalAsync(GridState<TEntity> state, CancellationToken token)
    {
        // АВТОМАТИКА ПЕРВИЧНОГО ПРОГРЕВА:
        // Если включен ОЗУ-режим (брокер это знает), но данные еще не предзагружены (коллекция пуста)
        if (ActionContext.DataBroker.IsInMemoryMode && ActionContext.DataBroker.InMemoryItems == null)
        {
            // Ищем метод прогрева на конкретном контексте страницы
            var initMethod = ActionContext.GetType().GetMethod("InitializeInMemoryDataAsync");
            if (initMethod != null)
            {
                // Вызываем его и ЖДЕМ (await) завершения. 
                // Так как этот метод выполняется внутри контекста ServerData, 
                // MudBlazor сам включит красивый лоадер, а меню страницы останется 100% отзывчивым!
                var task = (Task)initMethod.Invoke(ActionContext, null)!;
                await task;
            }
        }

        // Напрямую вызываем брокер данных, развернутый внутри нашего контекста
        return await ActionContext.DataBroker.FetchDataAsync(state, token);
    }

    protected void OnSelectedItemChanged(TEntity? newItem)
    {
        if (newItem == null && ActionContext.SelectedData != null) return;

        if (!EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, newItem))
        {
            ActionContext.SelectedData = newItem;
            StateHasChanged();
        }
    }

    protected string FormatSelectedRowStyle(TEntity item, int index)
    {
        return item == ActionContext.SelectedData
            ? "background-color: var(--mud-palette-action-disabled-background) !important; color: var(--mud-palette-text-primary) !important; font-weight: 500;"
            : string.Empty;
    }

    public Task ReloadServerDataAsync() => _grid != null ? _grid.ReloadServerData() : Task.CompletedTask;

    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnContextUpdated -= StateHasChanged;
            ActionContext.OnRefreshRequested -= HandleRefreshRequested;
        }
    }
}