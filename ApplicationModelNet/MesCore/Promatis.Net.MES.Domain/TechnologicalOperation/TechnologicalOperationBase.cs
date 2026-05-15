using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовая технологическая операция.
/// Наследует чистое не-дженерик дерево СУБД, убирая конфликты маппинга,
/// и предоставляет строго типизированный интерфейс для бизнес-логики.
/// </summary>
public abstract class TechnologicalOperationBase<T, TOLink, TPLink> : ReferenceTreeBase,
    ITechnologicalOperation, ITreeNode<T>
    where T : TechnologicalOperationBase<T, TOLink, TPLink> 
    where TOLink : class
    where TPLink : class
{
    public bool IsLeaf { get; set; } = true;

    public virtual ICollection<TOLink> UnitLinks { get; set; } = new List<TOLink>();
    public virtual ICollection<TPLink> ParameterLinks { get; set; } = new List<TPLink>();

    // Свойства теперь возвращают строго конечный тип T (например, TechnologicalOperation)
    public virtual T? Parent { get; set; }
    public virtual ICollection<T> Children { get; set; } = new List<T>();
}