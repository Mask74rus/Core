using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.BaseTree;

public partial class BaseTreePage<TEntity> : ComponentBase where TEntity : class
{
    // ИСПРАВЛЕНО: Декларируем новые параметры строго внутри .razor для мгновенной видимости компилятором
    [Parameter] public Func<TEntity, string>? ItemIconFunc { get; set; }
    [Parameter] public Func<TEntity, Color>? ItemIconColorFunc { get; set; }

    // В MudBlazor 9 тип коллекции для дерева обернут в TreeItemData
    [Parameter] public List<TreeItemData<TEntity>>? Items { get; set; }
    [Parameter] public Func<TEntity, Task<IReadOnlyCollection<TreeItemData<TEntity>>>>? ServerData { get; set; }
    [Parameter] public bool IsLoading { get; set; } = false;

    [Parameter] public RenderFragment? EmptyContent { get; set; }
    [Parameter] public RenderFragment<TEntity>? NodeContent { get; set; }
    [Parameter] public RenderFragment? AdditionalToolbarContent { get; set; }

    [Parameter] public TreeActionContext<TEntity> ActionContext { get; set; } = null!;

    // Функции обратного вызова для гибкой конфигурации дерева на страницах-наследниках
    [Parameter] public Func<TEntity, string>? ItemTextFunc { get; set; }
    [Parameter] public Func<TEntity, bool>? CanExpandFunc { get; set; }

    [Parameter] public EventCallback OnCreateRootTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnCreateChildTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditNodeTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteNodeTriggered { get; set; }

    [Parameter] public EventCallback<TEntity?> SelectedItemChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ActionContext ??= new TreeActionContext<TEntity>();
        // Синхронизируем перерисовку UI при изменении контекста
        ActionContext.OnContextUpdated = StateHasChanged;
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
        // Если кликнули мимо или объект пустой — ничего не делаем
        if (directNode == null) return;

        // Если пользователь повторно кликает по уже выбранному листу — игнорируем, 
        // не сбрасывая фокус и сохраняя тулбар активным
        if (EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, directNode))
        {
            return;
        }

        // Записываем железно существующий C#-объект в источник правды контекста
        ActionContext.SelectedData = directNode;

        if (SelectedItemChanged.HasDelegate)
        {
            await SelectedItemChanged.InvokeAsync(directNode);
        }
    }
}