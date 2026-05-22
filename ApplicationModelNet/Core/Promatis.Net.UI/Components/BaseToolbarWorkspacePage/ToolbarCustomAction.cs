using MudBlazor;

namespace Promatis.Net.UI.Components.BaseToolbarWorkspacePage;

/// <summary>
/// Глобальный платформенный класс для описания расширенных действий (кнопок) на тулбаре.
/// Приводит к единому стандарту выгрузку в Excel, печать, импорт и любые кастомные операции во всех модулях системы.
/// </summary>
public class ToolbarCustomAction
{
    /// <summary>
    /// Уникальный строковый идентификатор действия (например, "excel_export").
    /// Используется для поиска и программного изменения состояния кнопки из бэкенда.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Текст (наименование), отображаемый на кнопке.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Опциональная графическая иконка MudBlazor (например, Icons.Material.Filled.Download).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Цветовая схема кнопки из системной палитры MudBlazor (Primary, Success, Error и т.д.).
    /// </summary>
    public Color Color { get; set; } = Color.Default;

    /// <summary>
    /// Стиль отображения кнопки (Filled, Outlined, Text). По умолчанию залитая (Filled).
    /// </summary>
    public Variant Variant { get; set; } = Variant.Filled;

    /// <summary>
    /// Флаг видимости кнопки на тулбаре. Если false — кнопка полностью исключается из дерева рендеринга.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Флаг доступности кнопки для клика (активна/затушена).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Ссылка на реальный асинхронный метод бизнес-страницы, который выполнится в контексте этой страницы при нажатии.
    /// </summary>
    public required Func<Task> OnExecute { get; init; }
}