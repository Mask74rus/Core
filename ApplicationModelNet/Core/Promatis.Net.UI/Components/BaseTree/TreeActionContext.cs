namespace Promatis.Net.UI;

public class TreeActionContext : WorkspaceActionContext
{
    public bool IsCreateRootEnabled { get; set; } = true;
    public bool IsCreateChildEnabled { get; set; } = false;
    public bool IsDeleteNodeEnabled { get; set; } = false;

    public bool IsCreateRootVisible { get; set; } = true;
    public bool IsCreateChildVisible { get; set; } = true;
    public bool IsDeleteNodeVisible { get; set; } = true;
}