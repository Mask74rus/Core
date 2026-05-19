using MudBlazor;

namespace Promatis.Net.UI;

public class UiModule : IUiModule
{
    public string Name => GetType().Assembly.GetName().Name ?? "Unknown.Module";

    public IEnumerable<(string Title, string Href, string Icon, string? Group)> GetMenuItems()
    {
        return new List<(string Title, string Href, string Icon, string? Group)>
        {
            // Элемент верхнего уровня (вне папок)
            ("Главная", "/", Icons.Material.Filled.Dashboard, null),
            
            // Раздел системного администрирования
            ("Журнал аудита", "/admin/audit-logs", Icons.Material.Filled.History, "Администрирование"),

            ("Тест", "/mdm/all-units", Icons.Material.Filled.Dashboard, null),
        };
    }
}