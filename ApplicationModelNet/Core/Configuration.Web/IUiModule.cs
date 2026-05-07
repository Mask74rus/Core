namespace Promatis.Net.Configuration.Web;

// Интерфейс для модулей, которые хотят добавить свои пункты меню
public interface IUiModule
{
    string Name { get; }
    // Метод возвращает пункты меню (Название, Ссылка, Иконка)
    IEnumerable<(string Title, string Href, string Icon)> GetMenuItems();
}