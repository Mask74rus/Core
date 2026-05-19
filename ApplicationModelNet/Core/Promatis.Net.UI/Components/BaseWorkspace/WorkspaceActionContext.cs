namespace Promatis.Net.UI;

public class WorkspaceActionContext
{
    public string PageTitle { get; set; } = "Рабочая область";
    public ToolbarPosition Position { get; set; } = ToolbarPosition.Top;

    public Action? OnContextUpdated { get; set; }
    public void NotifyUpdate() => OnContextUpdated?.Invoke();
}