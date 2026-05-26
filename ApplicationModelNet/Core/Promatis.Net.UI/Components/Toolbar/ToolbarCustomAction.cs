using MudBlazor;

namespace Promatis.Net.UI.Components.Toolbar;

/// <summary>
/// Глобальный платформенный класс для декларативного описания расширенных (кастомных) кнопок на тулбаре.
/// Обеспечивает цветовое и визуальное единообразие во всех прикладных модулях системы.
/// </summary>
public class ToolbarCustomAction
{
    /// <summary>
    /// Уникальный строковый идентификатор действия (например, "excel_export").
    /// Используется для программного изменения состояния кнопки (включение/выключение) из бэкенда.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Текст (наименование), отображаемый на кнопке тулбара.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Графическая иконка MudBlazor (например, Icons.Material.Filled.Download).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Цветовая схема кнопки из системной палитры MudBlazor (Primary, Success, Error, Warning и т.д.).
    /// </summary>
    public Color Color { get; set; } = Color.Default;

    /// <summary>
    /// Стиль отображения кнопки (Filled, Outlined, Text). По умолчанию — залитая (Filled).
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
    /// Ссылка на реальный асинхронный метод бизнес-логики контекста, который выполнится при нажатии.
    /// </summary>
    public required Func<Task> OnExecute { get; init; }
}