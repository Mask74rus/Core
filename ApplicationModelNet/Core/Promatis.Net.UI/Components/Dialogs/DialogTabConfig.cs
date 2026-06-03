using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Dialogs;

/// <summary>
/// Строго типизированный класс-конфигуратор для динамических вкладок.
/// Позволяет передавать кастомную разметку полей ввода, привязанную к живой доменной модели.
/// </summary>
public class DialogTabConfig<TModel> where TModel : class
{
    /// <summary>
    /// Текстовый заголовок вкладки на панели MudTabs.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Системная иконка MudBlazor для заголовка вкладки (опционально).
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// Строго типизированная разметка полей ввода, принимающая живую доменную модель по ссылке.
    /// </summary>
    public RenderFragment<TModel> Content { get; }

    public DialogTabConfig(string title, RenderFragment<TModel> content, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Заголовок вкладки не может быть пустым.", nameof(title));

        Title = title;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Icon = icon;
    }
}