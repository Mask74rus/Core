using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class GridPage<TEntity> : ComponentBase where TEntity : class
{
    private MudDataGrid<TEntity>? _mudGrid;

    [CascadingParameter]
    protected IWorkspaceContext Context { get; set; } = null!;

    /// <summary>
    /// Прямой провайдер данных MudBlazor, транслируемый из DataContext.GetDataAsync.
    /// </summary>
    [Parameter]
    public Func<GridState<TEntity>, CancellationToken, Task<GridData<TEntity>>>? ServerDataProvider { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool WithPagination { get; set; }

    [Parameter]
    public int RowsPerPage { get; set; } = 10000;

    /// <summary>
    /// Обратный мост (Callback). Сигнализирует о факте выбора строки пользователем.
    /// Прикладная страница свяжет это событие напрямую со свойством Context.SelectedData.
    /// </summary>
    [Parameter]
    public Action<TEntity?>? OnRowSelected { get; set; }

    protected TEntity? SelectedRow { get; set; }

    /// <summary>
    /// Принудительное обновление данных таблицы (вызывается внешней страницей).
    /// </summary>
    public Task ReloadServerData() => _mudGrid != null ? _mudGrid.ReloadServerData() : Task.CompletedTask;

    /// <summary>
    /// Визуальная подсветка активного фокуса в браузере.
    /// </summary>
    protected string FormatSelectedRowStyle(TEntity item, int rowNumber)
        => item == SelectedRow ? "background-color: var(--mud-palette-action-disabled-background); font-weight: 500;" : string.Empty;

    /// <summary>
    /// Локальный перехват события клика MudBlazor.
    /// </summary>
    protected void OnSelectedRowChanged(TEntity? newSelection)
    {
        // ЮВЕЛИРНЫЙ UX-ШАГ: Сохраняем фокус, если пользователь случайно кликнул мимо строк
        if (newSelection == null && SelectedRow != null) return;

        SelectedRow = newSelection;

        // Просто декларируем факт действия. Решения принимает координатор верхнего уровня.
        OnRowSelected?.Invoke(newSelection);
    }
}