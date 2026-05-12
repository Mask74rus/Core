using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый класс технологической операции
/// </summary>
public abstract class TechnologicalOperationBase : ReferenceTreeBase
{
    /// <summary>
    /// Признак того, что это конечная операция (лист), а не группа.
    /// </summary>
    public bool IsLeaf { get; set; } = true;

    /// <summary>
    /// Коллекция связей с оборудованием. 
    /// Используем конкретный класс связи (неабстрактный, если это MDM).
    /// </summary>
    public virtual ICollection<TechnologicalOperationUnitBase> UnitLinks { get; set; } = new List<TechnologicalOperationUnitBase>();
}