using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class GridPage<TEntity> : ComponentBase where TEntity : class
{
    [CascadingParameter] protected IWorkspaceActionContext? ActionContext { get; set; }

    [Parameter] public IEnumerable<TEntity>? Items { get; set; }
    [Parameter] public RenderFragment? GridColumns { get; set; }

    protected TEntity? SelectedRow { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Синхронизация начального фокуса через канонический интерфейс
        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            SelectedRow = bindableContext.SelectedData;
        }
    }

    protected void OnSelectedRowChanged(TEntity? newSelection)
    {
        SelectedRow = newSelection;

        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            bindableContext.SelectedData = newSelection;
            bindableContext.OnContextUpdated?.Invoke(); // Посылаем импульс перерисовки тулбару
        }
    }
}