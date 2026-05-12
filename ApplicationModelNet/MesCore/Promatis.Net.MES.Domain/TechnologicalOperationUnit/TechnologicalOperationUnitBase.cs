using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для связи (расширяемая)
/// </summary>
public abstract class TechnologicalOperationUnitBase : DomainObject
{
    public Guid OperationId { get; set; }
    public virtual TechnologicalOperationBase Operation { get; set; } = null!;

    public Guid UnitId { get; set; }
    public virtual UnitBase Unit { get; set; } = null!;

    // Поля для расширения
    public int Priority { get; set; } = 1;
}