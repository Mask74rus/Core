using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для связи (расширяемая)
/// </summary>
public abstract class TechnologicalOperationUnitBase : DomainObject, ISoftDeletable
{
    public Guid OperationId { get; set; }
    public virtual TechnologicalOperationBase Operation { get; set; } = null!;

    public Guid UnitId { get; set; }
    public virtual UnitBase Unit { get; set; } = null!;

    public int Priority { get; set; } = 1;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}