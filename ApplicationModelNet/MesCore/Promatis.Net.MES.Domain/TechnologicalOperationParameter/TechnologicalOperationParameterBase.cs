using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Связующая сущность «Многие-ко-многим» между технологическими операциями и их параметрами.
/// Определяет требования к заполнению параметров для конкретной операции.
/// </summary>
public abstract class TechnologicalOperationParameterBase : DomainObject, ISoftDeletable
{
    public Guid OperationId { get; set; }

    /// <summary>
    /// Навигационное свойство к базовой операции.
    /// </summary>
    public virtual TechnologicalOperationBase Operation { get; set; } = null!;

    public Guid ParameterId { get; set; }

    /// <summary>
    /// Навигационное свойство к базовому параметру.
    /// </summary>
    public virtual TechnologicalParameterBase Parameter { get; set; } = null!;

    /// <summary>
    /// Признак того, что параметр обязателен для заполнения при выполнении данной операции.
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Номинальное значение параметра по умолчанию (в строковом виде для универсальности).
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Минимально допустимое значение (для числовых параметров).
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Максимально допустимое значение (для числовых параметров).
    /// </summary>
    public double? MaxValue { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
