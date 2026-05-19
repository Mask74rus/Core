namespace Promatis.Net.Configuration.Web;

public class TabItem
{
    public string Id { get; init; } = string.Empty; // Наш Href
    public string Title { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public Type ComponentType { get; init; } = null!;
}