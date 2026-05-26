using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Promatis.Net.UI.Components.Toolbar;

public partial class UiToolbar<TEntity> : ComponentBase where TEntity : class
{
    [Parameter] public ToolbarActionContext<TEntity> ActionContext { get; set; } = null!;
    [Parameter] public RenderFragment? AdditionalContent { get; set; }

    [Parameter] public EventCallback OnCreateTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnEditTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnDeleteTriggered { get; set; }
    [Parameter] public EventCallback<TEntity> OnCreateChildTriggered { get; set; }

    protected bool IsVertical => ActionContext?.Position == ToolbarPosition.Left || ActionContext?.Position == ToolbarPosition.Right;

    protected string ZoneSize => ActionContext?.Position switch
    {
        ToolbarPosition.Left => ActionContext.LeftZoneWidth,
        ToolbarPosition.Right => ActionContext.RightZoneWidth,
        ToolbarPosition.Top => ActionContext.TopZoneHeight,
        ToolbarPosition.Bottom => ActionContext.BottomZoneHeight,
        _ => "auto"
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext == null)
        {
            throw new ArgumentNullException(nameof(ActionContext),
                $"Компонент {nameof(UiToolbar<TEntity>)} требует обязательной передачи параметра {nameof(ActionContext)}.");
        }

        ActionContext.OnContextUpdated = StateHasChanged;
    }

    protected async Task OnCreateClick(MouseEventArgs e)
    {
        if (OnCreateTriggered.HasDelegate) await OnCreateTriggered.InvokeAsync();
    }

    protected async Task OnCreateChildClick(MouseEventArgs e)
    {
        if (OnCreateChildTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnCreateChildTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnEditClick(MouseEventArgs e)
    {
        if (OnEditTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnEditTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnDeleteClick(MouseEventArgs e)
    {
        if (OnDeleteTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnDeleteTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    /// <summary>
    /// Динамический фрагмент отрисовки кнопок, инкапсулированный в C#-классе бэкенда
    /// </summary>
    private RenderFragment RenderButtonsCollection => __builder =>
    {
        bool isCreateChildVisible = false;
        bool isCreateChildEnabled = false;

        int seq = 0;

        if (ActionContext.IsCreateVisible)
        {
            __builder.OpenComponent<MudButton>(seq++);
            __builder.AddAttribute(seq++, "Variant", Variant.Filled);
            __builder.AddAttribute(seq++, "Color", Color.Primary);
            __builder.AddAttribute(seq++, "Size", Size.Small);
            __builder.AddAttribute(seq++, "Disabled", !ActionContext.IsCreateEnabled);
            // ИСПРАВЛЕНО: Генерируем строго типизированный EventCallback<MouseEventArgs>
            __builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnCreateClick));
            __builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Создать")));
            __builder.CloseComponent();
        }

        if (isCreateChildVisible)
        {
            __builder.OpenComponent<MudButton>(seq++);
            __builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            __builder.AddAttribute(seq++, "Color", Color.Primary);
            __builder.AddAttribute(seq++, "Size", Size.Small);
            __builder.AddAttribute(seq++, "Disabled", !isCreateChildEnabled);
            // ИСПРАВЛЕНО: Генерируем строго типизированный EventCallback<MouseEventArgs>
            __builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnCreateChildClick));
            __builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Добавить подузел")));
            __builder.CloseComponent();
        }

        if (ActionContext.IsEditVisible)
        {
            __builder.OpenComponent<MudButton>(seq++);
            __builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            __builder.AddAttribute(seq++, "Color", Color.Info);
            __builder.AddAttribute(seq++, "Size", Size.Small);
            __builder.AddAttribute(seq++, "Disabled", !ActionContext.IsEditEnabled);
            // ИСПРАВЛЕНО: Генерируем строго типизированный EventCallback<MouseEventArgs>
            __builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnEditClick));
            __builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Изменить")));
            __builder.CloseComponent();
        }

        if (ActionContext.IsDeleteVisible)
        {
            __builder.OpenComponent<MudButton>(seq++);
            __builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            __builder.AddAttribute(seq++, "Color", Color.Error);
            __builder.AddAttribute(seq++, "Size", Size.Small);
            __builder.AddAttribute(seq++, "Disabled", !ActionContext.IsDeleteEnabled);
            // ИСПРАВЛЕНО: Генерируем строго типизированный EventCallback<MouseEventArgs>
            __builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnDeleteClick));
            __builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Удалить")));
            __builder.CloseComponent();
        }

        if (AdditionalContent != null)
        {
            if (ActionContext.IsCreateVisible || isCreateChildVisible || ActionContext.IsEditVisible || ActionContext.IsDeleteVisible)
            {
                __builder.OpenComponent<MudDivider>(seq++);
                __builder.AddAttribute(seq++, "Vertical", !IsVertical);
                __builder.AddAttribute(seq++, "FlexItem", true);
                __builder.AddAttribute(seq++, "Class", "mx-2 my-1");
                __builder.CloseComponent();
            }
            __builder.AddContent(seq++, AdditionalContent);
        }

        if (ActionContext.CustomActions.Any(a => a.IsVisible) &&
            (ActionContext.IsCreateVisible || isCreateChildVisible || ActionContext.IsEditVisible || ActionContext.IsDeleteVisible || AdditionalContent != null))
        {
            __builder.OpenComponent<MudDivider>(seq++);
            __builder.AddAttribute(seq++, "Vertical", !IsVertical);
            __builder.AddAttribute(seq++, "FlexItem", true);
            __builder.AddAttribute(seq++, "Class", "mx-2 my-1");
            __builder.CloseComponent();
        }

        foreach (ToolbarCustomAction action in ActionContext.CustomActions.Where(a => a.IsVisible))
        {
            __builder.OpenComponent<MudButton>(seq++);
            __builder.AddAttribute(seq++, "Variant", action.Variant);
            __builder.AddAttribute(seq++, "Color", action.Color);
            __builder.AddAttribute(seq++, "Size", Size.Small);
            __builder.AddAttribute(seq++, "StartIcon", action.Icon);
            __builder.AddAttribute(seq++, "Disabled", !action.IsEnabled);
            // Для CustomActions оставляем плоский вызов, так как у них в контракте Func<Task> без MouseEventArgs,
            // но фабрике явно указываем приведение к общему типу клика
            __builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, action.OnExecute));
            __builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, action.Title)));
            __builder.CloseComponent();
        }
    };
}