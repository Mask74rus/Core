using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Domain;

/// <summary>
/// Базовый класс для всех справочников содержащих деревья
/// </summary>
/// <summary>
/// Базовый класс для всех древовидных справочников (MDM).
/// Использует паттерн CRTP для передачи строго типизированных навигационных свойств в интерфейс ITreeNode.
/// </summary>
/// <typeparam name="T">Тип конкретного доменного объекта (наследника).</typeparam>
public abstract class ReferenceTreeBase<T> : ReferenceBase, ITreeNode<T> where T : class
{
    /// <summary>
    /// Идентификатор родительского узла в СУБД.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Навигационное свойство ссылки на родительский объект в оперативной памяти.
    /// Переопределяется как virtual в конечных классах для поддержки Lazy Loading.
    /// </summary>
    public virtual T? Parent { get; set; }

    /// <summary>
    /// Коллекция дочерних элементов (подчиненных узлов).
    /// </summary>
    public virtual ICollection<T> Children { get; set; } = new List<T>();
}