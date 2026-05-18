using MudBlazor;
using Promatis.Net.UI;

namespace Promatis.Net.MES.UI;

public class UiModule : IUiModule
{
    public string Name => GetType().Assembly.GetName().Name ?? "Unknown.Module";

    public IEnumerable<(string Title, string Href, string Icon, string? Group)> GetMenuItems()
    {
        return new List<(string Title, string Href, string Icon, string? Group)>
        {

            ("Вся структура ERP", "/mdm/all-units", Icons.Material.Filled.AccountTree, "Справочники")

        };
    }
}