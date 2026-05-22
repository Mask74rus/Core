using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.BaseTree;

public partial class BaseTreePage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    [Parameter] public Func<TEntity, string>? ItemIconFunc { get; set; }
    [Parameter] public Func<TEntity, Color>? ItemIconColorFunc { get; set; }

    [Parameter] public List<TreeItemData<TEntity>>? Items { get; set; }
    [Parameter] public Func<TEntity, Task<IReadOnlyCollection<TreeItemData<TEntity>>>>? ServerData { get; set; }
    [Parameter] public bool IsLoading { get; set; } = false;

    [Parameter] public RenderFragment? EmptyContent { get; set; }
    [Parameter] public RenderFragment<TEntity>? NodeContent { get; set; }
    [Parameter] public RenderFragment? AdditionalToolbarContent { get; set; }

    [Parameter] public TreeActionContext<TEntity> ActionContext { get; set; } = null!;

    [Parameter] public Func<TEntity, string>? ItemTextFunc { get; set; }
    [Parameter] public Func<TEntity, bool>? CanExpandFunc { get; set; }

    [Parameter] public EventCallback OnCreateRootTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnCreateChildTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditNodeTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteNodeTriggered { get; set; }

    [Parameter] public EventCallback<TEntity?> SelectedItemChanged { get; set; }

    // НОВЫЙ ТРИГГЕР: Вызывается, когда контекст сообщает о коммите сущности данного типа,
    // чтобы заставить бизнес-страницу перечитать полный граф в оперативной памяти.
    [Parameter] public EventCallback OnDataChangedRefreshRequested { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ActionContext ??= new TreeActionContext<TEntity>();

        // Синхронизируем перерисовку UI при изменении контекста
        ActionContext.OnContextUpdated = StateHasChanged;

        // РЕАКТИВНАЯ АВТОМАТИКА: Подписываемся на импульсы обновлений из контекста холста
        ActionContext.OnRefreshRequested += HandleRefreshRequest;
    }

    /// <summary>
    /// Перехватывает событие обновления из контекста и реактивно перерисовывает дерево
    /// </summary>
    private void HandleRefreshRequest()
    {
        InvokeAsync(async () =>
        {
            if (OnDataChangedRefreshRequested.HasDelegate)
            {
                // Просим бизнес-страницу (например, UnitTreePage) обновить коллекцию Items в памяти
                await OnDataChangedRefreshRequested.InvokeAsync();
            }

            StateHasChanged();
        });
    }

    protected async Task OnCreateRootClick()
    {
        if (OnCreateRootTriggered.HasDelegate) await OnCreateRootTriggered.InvokeAsync();
    }

    protected async Task OnCreateChildClick()
    {
        if (OnCreateChildTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnCreateChildTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnEditNodeClick()
    {
        if (OnEditNodeTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnEditNodeTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnDeleteNodeClick()
    {
        if (OnDeleteNodeTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnDeleteNodeTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    private async Task OnSelectedChanged(TEntity? directNode)
    {
        if (directNode == null) return;

        if (EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, directNode))
        {
            return;
        }

        ActionContext.SelectedData = directNode;

        if (SelectedItemChanged.HasDelegate)
        {
            await SelectedItemChanged.InvokeAsync(directNode);
        }
    }

    /// <summary>
    /// Освобождаем подписку при закрытии MDI-вкладки для защиты от утечек памяти
    /// </summary>
    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnRefreshRequested -= HandleRefreshRequest;
        }
    }
}