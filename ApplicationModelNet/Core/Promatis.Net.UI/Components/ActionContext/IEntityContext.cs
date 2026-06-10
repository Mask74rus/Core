namespace Promatis.Net.UI.Components;


/// <summary>
/// Универсальный интерфейс контекста сущности для работы с операциями выбора (Selection).
/// Предоставляет не-generic доступ к выделенной строке для пассивных визуализаторов (RenderBase).
/// </summary>
public interface IEntityContext : IDataContext
{
    /// <summary>
    /// Выбранная в данный момент строка таблицы или узел дерева (в виде object?).
    /// </summary>
    object? SelectedData { get; set; }
}