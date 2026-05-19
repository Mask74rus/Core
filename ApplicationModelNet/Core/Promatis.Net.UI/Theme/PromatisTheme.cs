using MudBlazor;

namespace Promatis.Net.Ui.Theme;

public static class PromatisTheme
{
    public static MudTheme DefaultTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Lighten1,       // Главный корпоративный цвет (кнопки, активные элементы, фокус)
            Secondary = Colors.Blue.Lighten2,     // Вспомогательный цвет (второстепенные элементы, чипы)

            AppbarBackground = "#EAEAEA",         // Фон верхней панели (MudAppBar) в дневном режиме
            AppbarText = "#2C3E50",               // Цвет текста, заголовков и иконок в верхней панели
            DrawerBackground = "#EAEAEA",         // Фон бокового меню навигации (MudDrawer)
            Background = "#F9F9F9",               // Общий фоновый цвет подложки всех страниц приложения
            Surface = Colors.Shades.White         // Фон контейнеров MudPaper, карточек MudCard и таблиц
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Blue.Lighten1,       // Главный цвет в ночном режиме (мягкий синий)
            Secondary = Colors.Blue.Lighten2,     // Вспомогательный цвет в ночном режиме

            AppbarBackground = "#0D0D0D",         // Фон верхней панели (MudAppBar) в глубоком чёрном стиле
            AppbarText = Colors.Shades.White,     // Цвет текста и иконок в шапке (контрастный белый)

            Background = "#121212",               // Глубокий тёмный фон страниц подложки
            Surface = "#1E1E1E",                  // Графитовый фон для MudPaper, контента вкладок и таблиц
            DrawerBackground = "#1E1E1E",         // Фон боковой панели навигации в тёмной теме
            DrawerText = "#E0E0E0",               // Цвет ссылок и текста в боковом меню
            TextPrimary = "#E0E0E0",              // Основной цвет текста на страницах (мягкий светло-серый)
            TextSecondary = "#A0A0A0"             // Цвет для второстепенного текста и подсказок
        },
        LayoutProperties = new LayoutProperties
        {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        }
    };
}
