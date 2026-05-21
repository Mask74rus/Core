using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalParameterCalcMethodBase<TUnit, TOperation, TParameter> : DomainObject, ISoftDeletable
    where TUnit : UnitBase
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
{
    public Guid UnitId { get; set; }

    /// <summary>
    /// Цеховая единица
    /// </summary>
    public virtual TUnit Unit { get; set; } = null!;

    public Guid TechnologicalOperationId { get; set; }

    /// <summary>
    /// Технологическая операция
    /// </summary>
    public virtual TOperation TechnologicalOperation { get; set; } = null!;

    public Guid TechnologicalParameterId { get; set; }

    /// <summary>
    /// Технологический параметр
    /// </summary>
    public virtual TParameter TechnologicalParameter { get; set; } = null!;

    /// <summary>
    /// Метод расчета
    /// </summary>
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.None;

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}