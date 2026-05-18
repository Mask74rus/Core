namespace Promatis.Net.UI;

/// <summary>
/// Контракт для динамических модулей пользовательского интерфейса.
/// </summary>
public interface IUiModule
{
    /// <summary>
    /// Отображаемое имя модуля в системе (используется для сканера).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Возвращает список элементов навигации для бокового меню MudDrawer.
    /// Четвертый параметр отвечает за 2-уровневую группировку элементов.
    /// </summary>
    /// <returns>Коллекция кортежей: (Название, Ссылка, Иконка, НазваниеГруппы)</returns>
    IEnumerable<(string Title, string Href, string Icon, string? Group)> GetMenuItems();
}