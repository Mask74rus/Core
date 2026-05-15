using MudBlazor;

namespace Promatis.Net.Ui.Theme;

public static class PromatisTheme
{
    public static MudTheme DefaultTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Lighten1,
            Secondary = Colors.Blue.Lighten2,
            AppbarBackground = Colors.Red.Default,
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Lighten1
        },
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        }
    };
}