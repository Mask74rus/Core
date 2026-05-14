using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.ComponentModel.DataAnnotations.Schema;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовая технологическая операция.
/// Наследует чистое не-дженерик дерево СУБД, убирая конфликты маппинга,
/// и предоставляет строго типизированный интерфейс для бизнес-логики.
/// </summary>
public abstract class TechnologicalOperationBase<TOLink, TPLink> : ReferenceTreeBase,
    ITechnologicalOperation,
    ITreeNode<TechnologicalOperationBase<TOLink, TPLink>>
    where TOLink : class
    where TPLink : class
{
    /// <summary>
    /// Признак того, что это конечная операция (лист), а не группа операций.
    /// </summary>
    public bool IsLeaf { get; set; } = true;

    /// <summary>
    /// Коллекция связей с производственным оборудованием (юнитами).
    /// </summary>
    public virtual ICollection<TOLink> UnitLinks { get; set; } = new List<TOLink>();

    /// <summary>
    /// Коллекция контролируемых технологических параметров для данной операции.
    /// </summary>
    public virtual ICollection<TPLink> ParameterLinks { get; set; } = new List<TPLink>();

    /// <summary>
    /// Строго типизированный родитель для использования в сервисах. Не мапится в БД.
    /// </summary>
    [NotMapped]
    public TechnologicalOperationBase<TOLink, TPLink>? TypedParent => Parent as TechnologicalOperationBase<TOLink, TPLink>;

    /// <summary>
    /// Строго типизированные дочерние операции для использования в сервисах. Не мапится в БД.
    /// </summary>
    [NotMapped]
    public IEnumerable<TechnologicalOperationBase<TOLink, TPLink>> TypedChildren => Children.Cast<TechnologicalOperationBase<TOLink, TPLink>>();
}