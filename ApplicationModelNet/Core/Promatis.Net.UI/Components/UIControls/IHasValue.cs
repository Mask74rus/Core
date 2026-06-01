namespace Promatis.Net.UI.Components;

/// <summary>
/// Контракт для элементов управления, которые хранят и изменяют рантайм-значение (чекбоксы, селекты, пикеры).
/// </summary>
public interface IHasValue : IUiControl
{
    object? Value { get; set; }
}