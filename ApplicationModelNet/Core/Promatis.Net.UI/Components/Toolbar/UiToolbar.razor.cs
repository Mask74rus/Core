using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Promatis.Net.UI.Components.Tree;

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
    private RenderFragment RenderButtonsCollection => builder =>
    {
        bool isTreeMode = ActionContext.GetType().BaseType != null
                         && ActionContext.GetType().BaseType!.IsGenericType
                         && ActionContext.GetType().BaseType!.GetGenericTypeDefinition() == typeof(TreeActionContext<>);

        bool isCreateChildVisible = false;
        bool isCreateChildEnabled = false;

        // Если рантайм определил, что перед нами контекст дерева — извлекаем метаданные кнопок напрямую через апкаст
        if (isTreeMode)
        {
            dynamic? treeContext = ActionContext;
            if (treeContext != null)
            {
                isCreateChildVisible = treeContext.IsCreateChildVisible;
                isCreateChildEnabled = treeContext.IsCreateChildEnabled;
            }
        }

        int seq = 0;

        if (ActionContext.IsCreateVisible)
        {
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Filled);
            builder.AddAttribute(seq++, "Color", Color.Primary);
            builder.AddAttribute(seq++, "Size", Size.Small);
            builder.AddAttribute(seq++, "Disabled", !ActionContext.IsCreateEnabled);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnCreateClick));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Создать")));
            builder.CloseComponent();
        }

        // РЕНДЕР КНОПКИ ДЕРЕВА:
        if (isCreateChildVisible)
        {
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            builder.AddAttribute(seq++, "Color", Color.Primary);
            builder.AddAttribute(seq++, "Size", Size.Small);
            builder.AddAttribute(seq++, "Disabled", !isCreateChildEnabled);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnCreateChildClick));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Добавить подузел")));
            builder.CloseComponent();
        }

        if (ActionContext.IsEditVisible)
        {
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            builder.AddAttribute(seq++, "Color", Color.Info);
            builder.AddAttribute(seq++, "Size", Size.Small);
            builder.AddAttribute(seq++, "Disabled", !ActionContext.IsEditEnabled);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnEditClick));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Изменить")));
            builder.CloseComponent();
        }

        if (ActionContext.IsDeleteVisible)
        {
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", Variant.Outlined);
            builder.AddAttribute(seq++, "Color", Color.Error);
            builder.AddAttribute(seq++, "Size", Size.Small);
            builder.AddAttribute(seq++, "Disabled", !ActionContext.IsDeleteEnabled);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, OnDeleteClick));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, "Удалить")));
            builder.CloseComponent();
        }

        if (AdditionalContent != null)
        {
            if (ActionContext.IsCreateVisible || isCreateChildVisible || ActionContext.IsEditVisible || ActionContext.IsDeleteVisible)
            {
                builder.OpenComponent<MudDivider>(seq++);
                builder.AddAttribute(seq++, "Vertical", !IsVertical);
                builder.AddAttribute(seq++, "FlexItem", true);
                builder.AddAttribute(seq++, "Class", "mx-2 my-1");
                builder.CloseComponent();
            }
            builder.AddContent(seq++, AdditionalContent);
        }

        if (ActionContext.CustomActions.Any(a => a.IsVisible) &&
            (ActionContext.IsCreateVisible || isCreateChildVisible || ActionContext.IsEditVisible || ActionContext.IsDeleteVisible || AdditionalContent != null))
        {
            builder.OpenComponent<MudDivider>(seq++);
            builder.AddAttribute(seq++, "Vertical", !IsVertical);
            builder.AddAttribute(seq++, "FlexItem", true);
            builder.AddAttribute(seq++, "Class", "mx-2 my-1");
            builder.CloseComponent();
        }

        foreach (ToolbarCustomAction action in ActionContext.CustomActions.Where(a => a.IsVisible))
        {
            builder.OpenComponent<MudButton>(seq++);
            builder.AddAttribute(seq++, "Variant", action.Variant);
            builder.AddAttribute(seq++, "Color", action.Color);
            builder.AddAttribute(seq++, "Size", Size.Small);
            builder.AddAttribute(seq++, "StartIcon", action.Icon);
            builder.AddAttribute(seq++, "Disabled", !action.IsEnabled);
            builder.AddAttribute(seq++, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, action.OnExecute));
            builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(seq++, action.Title)));
            builder.CloseComponent();
        }
    };
}