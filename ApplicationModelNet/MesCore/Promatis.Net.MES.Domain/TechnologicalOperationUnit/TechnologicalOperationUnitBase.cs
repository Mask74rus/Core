using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для связи (расширяемая)
/// </summary>
public abstract class TechnologicalOperationUnitBase<TOperation> : DomainObject, ISoftDeletable
    where TOperation : DomainObject, ITechnologicalOperation
{
    public Guid OperationId { get; set; }

    /// <summary>
    /// Навигационное свойство к операции (будет закрыто конкретным типом в MDM).
    /// </summary>
    public virtual TOperation Operation { get; set; } = null!;

    public Guid UnitId { get; set; }

    /// <summary>
    /// Прямая ссылка на базовый класс оборудования. 
    /// EF Core автоматически свяжет это с таблицей оборудования по соглашениям.
    /// </summary>
    public virtual UnitBase Unit { get; set; } = null!;

    public int Priority { get; set; } = 1;

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}