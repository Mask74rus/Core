using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Grid;

public partial class GridPage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    [Parameter] public RenderFragment? ColumnsContent { get; set; }
    [Parameter] public RenderFragment? PagerContent { get; set; }

    /// <summary>
    /// Ловим табличный контекст из каскадного потока холста WorkspacePage.
    /// </summary>
    [CascadingParameter] protected GridActionContext<TEntity> ActionContext { get; set; } = null!;

    private MudDataGrid<TEntity> _grid = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext == null)
        {
            throw new ArgumentNullException(nameof(ActionContext),
                $"Компонент {nameof(GridPage<TEntity>)} требует наличия {nameof(GridActionContext<TEntity>)} в каскадных параметрах холста.");
        }

        // Связываем обновление фокуса и триггеры перерисовки таблицы с контекстом
        ActionContext.OnContextUpdated += StateHasChanged;

        // Подписываемся на импульс обновления данных (например, при программной смене фильтров в контексте)
        ActionContext.OnRefreshRequested += HandleRefreshRequested;
    }

    /// <summary>
    /// Обработчик импульса обновления данных от контекста страницы.
    /// </summary>
    private void HandleRefreshRequested() 
        => InvokeAsync(ReloadServerDataAsync);

    /// <summary>
    /// Внутренний метод-мост, перенаправляющий запрос MudBlazor напрямую в инкапсулированный брокер данных.
    /// </summary>
    protected Task<GridData<TEntity>> LoadGridDataInternalAsync(GridState<TEntity> state, CancellationToken token) 
        => ActionContext.DataBroker.FetchDataAsync(state, token);

    /// <summary>
    /// Обработчик клика по строке. Передает выделенный элемент в контекст «полноты власти».
    /// </summary>
    protected void OnSelectedItemChanged(TEntity? newItem)
    {
        if (newItem == null && ActionContext.SelectedData != null)
        {
            return; // Игнорируем нативное зануление фокуса при клике мимо текста ячейки
        }

        if (!EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, newItem))
        {
            ActionContext.SelectedData = newItem;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Функция окрашивания выделенной строки через CSS-переменные палитры MudBlazor.
    /// </summary>
    protected string FormatSelectedRowStyle(TEntity item, int index)
    {
        return item == ActionContext.SelectedData
            ? "background-color: var(--mud-palette-action-disabled-background) !important; color: var(--mud-palette-text-primary) !important; font-weight: 500;"
            : string.Empty;
    }

    /// <summary>
    /// Принудительный перезапрос данных таблицы.
    /// </summary>
    public Task ReloadServerDataAsync() => _grid != null ? _grid.ReloadServerData() : Task.CompletedTask;

    /// <summary>
    /// Обязательная отписка от событий контекста для защиты от утечек памяти.
    /// </summary>
    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnContextUpdated -= StateHasChanged;
            ActionContext.OnRefreshRequested -= HandleRefreshRequested;
        }
    }
}