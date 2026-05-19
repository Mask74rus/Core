namespace Promatis.Net.Configuration.Web;

public class TabNavigationService
{
    public List<TabItem> OpenTabs { get; } = new();
    public int ActiveTabIndex { get; set; }
    public event Action? OnTabsChanged;

    public void OpenTab(string title, string href, string icon, Type componentType)
    {
        TabItem? existingTab = OpenTabs.FirstOrDefault(t => t.Id.Equals(href, StringComparison.OrdinalIgnoreCase));

        if (existingTab != null)
        {
            ActiveTabIndex = OpenTabs.IndexOf(existingTab);
        }
        else
        {
            var newTab = new TabItem
            {
                Id = href,
                Title = title,
                Icon = icon,
                ComponentType = componentType
            };

            OpenTabs.Add(newTab);
            ActiveTabIndex = OpenTabs.Count - 1;
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Безопасное закрытие вкладки по её числовому индексу
    /// </summary>
    public void CloseTabByIndex(int index)
    {
        if (index < 0 || index >= OpenTabs.Count) return;

        OpenTabs.RemoveAt(index);

        // Интеллектуальный расчет фокуса на оставшиеся вкладки
        if (ActiveTabIndex >= OpenTabs.Count)
        {
            ActiveTabIndex = OpenTabs.Count - 1;
        }

        if (ActiveTabIndex < 0 && OpenTabs.Count > 0)
        {
            ActiveTabIndex = 0;
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnTabsChanged?.Invoke();
}