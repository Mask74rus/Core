using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.EditDialog;

/// <summary>
/// Описание декларативной вкладки для универсального диалога редактирования
/// </summary>
public class DialogTab
{
    /// <summary>
    /// Отображаемое текстовое наименование вкладки (например, "Основное", "Спецификация")
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Ссылка на графическую иконку MudBlazor для вкладки
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Сама разметка полей ввода, инкапсулированная в виде компонента-вкладки
    /// </summary>
    public required RenderFragment Content { get; init; }
}