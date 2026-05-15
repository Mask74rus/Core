using MudBlazor;

namespace Promatis.Net.UI;

public class UiModule : IUiModule
{
    public string Name => "Управление НСИ (MDM)";

    public IEnumerable<(string Title, string Href, string Icon)> GetMenuItems()
    {
        return new List<(string Title, string Href, string Icon)>
        {
            // Корневая страница модуля
            ("Главная MDM", "/", Icons.Material.Filled.Dashboard),
        };
    }
}