namespace Promatis.Net.UI.Components;

/// <summary>
/// Универсальный контракт элемента управления (кнопка, чекбокс, выпадающий список).
/// </summary>
public interface IUiControl
{
    string Id { get; }
    Type ComponentType { get; }
    Dictionary<string, object> ComponentParameters { get; }

    string? Title { get; }
    string? Icon { get; }
    string? Tooltip { get; }

    bool IsVisible { get; set; }
    bool IsEnabled { get; set; }
    bool IsRunning { get; }

    bool IsEnabledForData(object? targetData);
    Task TriggerAsync(object? targetData);

    event Action? OnStateChanged;
}
