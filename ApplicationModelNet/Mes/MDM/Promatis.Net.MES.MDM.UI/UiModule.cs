using MudBlazor;
using Promatis.Net.UI;

namespace Promatis.Net.MES.MDM.UI;

public class UiModule : IUiModule
{
    public string Name => GetType().Assembly.GetName().Name ?? "Unknown.Module";

    public IEnumerable<(string Title, string Href, string Icon, string? Group)> GetMenuItems()
    {
        return new List<(string Title, string Href, string Icon, string? Group)>
        {
            // Справочник технологических операций
            ("Технологические операции", "/mes/mdm/technological-operations", Icons.Material.Filled.SettingsInputComponent, "Справочники"),

            // Плоский справочник технологических параметров
            ("Технологические параметры", "/mes/mdm/technological-parameters", Icons.Material.Filled.SettingsInputComponent, "Справочники")
        };
    }
}