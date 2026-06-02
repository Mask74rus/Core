using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class GridPage<TEntity> : ComponentBase where TEntity : class
{
    private MudDataGrid<TEntity>? _mudGrid;

    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    /// <summary>
    /// Фоновое серверное событие загрузки данных (мапится на MudBlazor ServerData).
    /// </summary>
    [Parameter]
    public Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? ServerDataProvider { get; set; }

    [Parameter]
    public RenderFragment? GridColumns { get; set; }

    [Parameter]
    public bool WithPagination { get; set; } = false;

    [Parameter]
    public int RowsPerPage { get; set; } = 10000;

    protected TEntity? SelectedRow { get; set; }

    /// <summary>
    /// Публичный метод, позволяющий страницам-владельцам принудительно обновить грид при смене фильтров.
    /// </summary>
    public Task ReloadServerData()
    {
        return _mudGrid != null ? _mudGrid.ReloadServerData() : Task.CompletedTask;
    }

    protected string FormatSelectedRowStyle(TEntity item, int rowNumber)
        => item == SelectedRow ? "background-color: var(--mud-palette-action-disabled-background); font-weight: 500;" : string.Empty;

    protected void OnSelectedRowChanged(TEntity? newSelection)
    {
        if (newSelection == null && SelectedRow != null) return;

        SelectedRow = newSelection;

        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            bindableContext.SelectedData = newSelection;
            bindableContext.OnContextUpdated?.Invoke();
        }
    }
}