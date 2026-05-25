using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовая технологическая операция.
/// Наследует чистое не-дженерик дерево СУБД, убирая конфликты маппинга,
/// и предоставляет строго типизированный интерфейс для бизнес-логики.
/// </summary>
public abstract class TechnologicalOperationBase<T, TOLink, TPLink> : ReferenceTreeBase<T>, // <- КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ
    ITechnologicalOperation
    where T : TechnologicalOperationBase<T, TOLink, TPLink>
    where TOLink : class
    where TPLink : class
{
    public bool IsLeaf { get; set; } = true;

    public virtual ICollection<TOLink> UnitLinks { get; set; } = new List<TOLink>();
    public virtual ICollection<TPLink> ParameterLinks { get; set; } = new List<TPLink>();
}