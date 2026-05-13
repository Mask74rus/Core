using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовая технологическая операция.
/// Наследует чистое не-дженерик дерево СУБД, убирая конфликты маппинга,
/// и предоставляет строго типизированный интерфейс для бизнес-логики.
/// </summary>
public abstract class TechnologicalOperationBase : ReferenceTreeBase, ITreeNode<TechnologicalOperationBase>
{
    /// <summary>
    /// Признак того, что это конечная операция (лист), а не группа операций.
    /// </summary>
    public bool IsLeaf { get; set; } = true;

    /// <summary>
    /// Коллекция связей с производственным оборудованием (юнитами). 
    /// Ссылается на базовую абстракцию связи Many-to-Many.
    /// </summary>
    public virtual ICollection<TechnologicalOperationUnitBase> UnitLinks { get; set; } = new List<TechnologicalOperationUnitBase>();

    /// <summary>
    /// Коллекция контролируемых технологических параметров для данной операции.
    /// </summary>
    public virtual ICollection<TechnologicalOperationParameterBase> ParameterLinks { get; set; } = new List<TechnologicalOperationParameterBase>();


    /// <summary>
    /// Строго типизированный родитель для использования в сервисах. Не мапится в БД.
    /// </summary>
    [NotMapped]
    public TechnologicalOperationBase? TypedParent => Parent as TechnologicalOperationBase;

    /// <summary>
    /// Строго типизированные дочерние операции для использования в сервисах. Не мапится в БД.
    /// </summary>
    [NotMapped]
    public IEnumerable<TechnologicalOperationBase> TypedChildren => Children.Cast<TechnologicalOperationBase>();
}