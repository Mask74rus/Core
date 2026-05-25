using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Data;

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

    [Parameter] public EventCallback OnCreateRootTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnCreateChildTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditNodeTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteNodeTriggered { get; set; }

    [Parameter] public EventCallback<TEntity?> SelectedItemChanged { get; set; }

    /// <summary>
    /// Сигнал инкрементального обновления для ОЗУ-движка бизнес-страницы.
    /// </summary>
    [Parameter] public EventCallback<(EntityStateChangeEnum State, TEntity Entity)> OnIncrementalUpdateRequested { get; set; }

    private TEntity? _selectedTreeValue;

    protected TEntity? SelectedTreeValue
    {
        get => _selectedTreeValue;
        set
        {
            if (!EqualityComparer<TEntity>.Default.Equals(_selectedTreeValue, value))
            {
                _selectedTreeValue = value;
                _ = OnSelectedChanged(value);
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ActionContext ??= new TreeActionContext<TEntity>();
        ActionContext.OnContextUpdated = StateHasChanged;

        // Прямое подключение к каналу СУБД
        DatabaseTriggerService.OnEntityCommitted += HandleEntityCommitted;
    }

    private void HandleEntityCommitted(EntityStateChangeEnum state, object entity)
    {
        Type entityType = entity.GetType();
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            var targetEntity = (TEntity)entity;

            InvokeAsync(async () =>
            {
                if (OnIncrementalUpdateRequested.HasDelegate)
                {
                    // Пинаем инкрементальный ОЗУ-движок на бизнес-странице
                    await OnIncrementalUpdateRequested.InvokeAsync((state, targetEntity));
                }
                StateHasChanged();
            });
        }
    }

    protected async Task OnSelectedChanged(TEntity? directNode)
    {
        if (directNode == null)
        {
            ActionContext.SelectedData = null;
            if (SelectedItemChanged.HasDelegate) await SelectedItemChanged.InvokeAsync(null);
            StateHasChanged();
            return;
        }

        if (EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, directNode))
        {
            return;
        }

        ActionContext.SelectedData = directNode;

        if (SelectedItemChanged.HasDelegate)
        {
            await SelectedItemChanged.InvokeAsync(directNode);
        }

        StateHasChanged();
    }

    protected async Task OnCreateRootClick() { if (OnCreateRootTriggered.HasDelegate) await OnCreateRootTriggered.InvokeAsync(); }
    protected async Task OnCreateChildClick() { if (OnCreateChildTriggered.HasDelegate && ActionContext.SelectedData != null) await OnCreateChildTriggered.InvokeAsync(ActionContext.SelectedData); }
    protected async Task OnEditNodeClick() { if (OnEditNodeTriggered.HasDelegate && ActionContext.SelectedData != null) await OnEditNodeTriggered.InvokeAsync(ActionContext.SelectedData); }
    protected async Task OnDeleteNodeClick() { if (OnDeleteNodeTriggered.HasDelegate && ActionContext.SelectedData != null) await OnDeleteNodeTriggered.InvokeAsync(ActionContext.SelectedData); }

    public void Dispose()
    {
        DatabaseTriggerService.OnEntityCommitted -= HandleEntityCommitted;
    }
}