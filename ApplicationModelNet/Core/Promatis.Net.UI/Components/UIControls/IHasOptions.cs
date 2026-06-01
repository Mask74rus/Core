namespace Promatis.Net.UI.Components;

/// <summary>
/// Контракт для элементов управления, обладающих списком доступных опций выбора (выпадающие списки).
/// </summary>
public interface IHasOptions : IUiControl
{
    IEnumerable<string> Options { get; }
}