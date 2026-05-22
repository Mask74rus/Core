namespace Promatis.Net.UI.Components.BaseToolbarWorkspacePage;

/// <summary>
/// Интерфейс-маркер для областей, имеющих командный тулбар управления
/// </summary>
public interface IHasToolbar
{
    ToolbarPosition Position { get; set; }
    bool IsToolbarVisible { get; set; }
}