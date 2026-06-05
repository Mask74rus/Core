using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class WorkspacePage : ComponentBase
{
    [CascadingParameter]
    protected IWorkspaceContext Context { get; set; } = null!;

    [Parameter]
    public RenderFragment? TopContent { get; set; }

    [Parameter]
    public RenderFragment? LeftContent { get; set; }

    [Parameter]
    public required RenderFragment BodyContent { get; set; }

    [Parameter]
    public RenderFragment? RightContent { get; set; }

    [Parameter]
    public RenderFragment? BottomContent { get; set; }
}