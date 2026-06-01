using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class WorkspacePage : ComponentBase, IDisposable
{
    /// <summary>
    /// Каскадный интерфейс контекста текущей рабочей области.
    /// </summary>
    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    [Parameter] public RenderFragment? BodyContent { get; set; }
    [Parameter] public RenderFragment? TopContent { get; set; }
    [Parameter] public RenderFragment? BottomContent { get; set; }
    [Parameter] public RenderFragment? LeftContent { get; set; }
    [Parameter] public RenderFragment? RightContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Подписка на реактивное обновление параметров геометрии
        if (ActionContext != null)
        {
            ActionContext.OnContextStateChanged += HandleContextStateChanged;
        }
    }

    private void HandleContextStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnContextStateChanged -= HandleContextStateChanged;
        }
    }

    // --- МЕТОДЫ РАСЧЕТА ВИЗУАЛЬНОГО СТИЛЯ ПОДЛОЖКИ ---
    protected int GetPaperElevation() => ActionContext?.PaperElevation ?? 1;
    protected string GetPaperClass() => ActionContext?.PaperClass ?? "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";

    // --- МЕТОДЫ РАСЧЕТА ГЕОМЕТРИИ КАРКАСА ХОЛСТА ---
    protected string GetWorkspaceHeight() => ActionContext?.WorkspaceHeight ?? "100%";
    protected string GetTopHeight() => ActionContext?.TopZoneHeight ?? "auto";
    protected string GetBottomHeight() => ActionContext?.BottomZoneHeight ?? "auto";
    protected string GetLeftWidth() => ActionContext?.LeftZoneWidth ?? "250px";
    protected string GetRightWidth() => ActionContext?.RightZoneWidth ?? "300px";

    protected bool IsTopCollapsed() => ActionContext?.IsTopZoneCollapsed ?? false;
    protected bool IsBottomCollapsed() => ActionContext?.IsBottomZoneCollapsed ?? false;
    protected bool IsLeftCollapsed() => ActionContext?.IsLeftZoneCollapsed ?? false;
    protected bool IsRightCollapsed() => ActionContext?.IsRightZoneCollapsed ?? false;
}