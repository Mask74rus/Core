namespace Promatis.Net.UI;

public class GridActionContext : WorkspaceActionContext
{
    public bool IsCreateEnabled { get; set; } = true;
    public bool IsDeleteEnabled { get; set; } = false;

    public bool IsCreateVisible { get; set; } = true;
    public bool IsDeleteVisible { get; set; } = true;
}