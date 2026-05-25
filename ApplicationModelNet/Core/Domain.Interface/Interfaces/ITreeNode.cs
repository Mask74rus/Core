namespace Promatis.Net.Domain.Interface;


/// <summary>
/// Глобальный платформенный контракт для всех иерархических (древовидных) сущностей.
/// </summary>
/// <typeparam name="T">Тип доменной модели.</typeparam>
public interface ITreeNode<T> : IDomainObjectHasKey<Guid> where T : class
{
    /// <summary>
    /// Идентификатор родительского узла. Равен null для корневых элементов.
    /// ИСПРАВЛЕНО: Добавлен set для возможности изменения связей (перемещения веток СУБД).
    /// </summary>
    Guid? ParentId { get; set; }

    /// <summary>
    /// Навигационное свойство ссылки на родительский объект.
    /// </summary>
    T? Parent { get; set; }

    /// <summary>
    /// Коллекция дочерних элементов (подчиненных узлов).
    /// </summary>
    ICollection<T> Children { get; }
}