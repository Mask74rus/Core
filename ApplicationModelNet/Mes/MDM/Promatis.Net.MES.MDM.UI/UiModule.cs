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
            // Плоский справочник технологических параметров в модуле MesMDM
            ("Технологические параметры", "/mes/mdm/technological-parameters", Icons.Material.Filled.SettingsInputComponent, "Справочники")
        };
    }
}