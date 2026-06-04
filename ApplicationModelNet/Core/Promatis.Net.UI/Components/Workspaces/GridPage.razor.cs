using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class GridPage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    private MudDataGrid<TEntity>? _mudGrid;
    private IWorkspaceContext? _currentContext;

    [CascadingParameter]
    protected IWorkspaceContext? ActionContext { get; set; }

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
    /// Контроль подписок на изменения контекста для защиты от утечек памяти.
    /// Переподписывается на лету, если Blazor подменит CascadingParameter контекста.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ActionContext != _currentContext)
        {
            if (_currentContext != null)
            {
                _currentContext.OnContextStateChanged -= HandleContextStateChanged;
            }

            _currentContext = ActionContext;

            if (ActionContext != null)
            {
                ActionContext.OnContextStateChanged += HandleContextStateChanged;
            }
        }
    }

    /// <summary>
    /// Реагирует на любые изменения состояния контекста (например, включение/выключение IsLoading).
    /// </summary>
    private void HandleContextStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public Task ReloadServerData()
    {
        return _mudGrid != null ? _mudGrid.ReloadServerData() : Task.CompletedTask;
    }

    protected string FormatSelectedRowStyle(TEntity item, int rowNumber)
        => item == SelectedRow ? "background-color: var(--mud-palette-action-disabled-background); font-weight: 500;" : string.Empty;

    protected void OnSelectedRowChanged(TEntity? newSelection)
    {
        // СОХРАНЕНО: Ваш намеренно спроектированный UX-шаг фиксации фокуса
        if (newSelection == null && SelectedRow != null) return;

        SelectedRow = newSelection;

        // Транслируем стейт напрямую в ядро. Контекст сам синхронно обновит состояние кнопок тулбара!
        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            bindableContext.SelectedData = newSelection;
        }
    }

    public void Dispose()
    {
        if (_currentContext != null)
        {
            _currentContext.OnContextStateChanged -= HandleContextStateChanged;
        }
    }
}