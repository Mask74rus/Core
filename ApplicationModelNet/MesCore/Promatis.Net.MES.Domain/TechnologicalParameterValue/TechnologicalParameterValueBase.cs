using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalParameterValueBase<TUnit, TParameter> : DomainObject
    where TUnit : UnitBase
    where TParameter : TechnologicalParameterBase
{
    public Guid UnitId { get; set; }

    /// <summary>
    /// Цеховая единица (источник данных)
    /// </summary>
    public virtual TUnit Unit { get; set; } = null!;

    public Guid TechnologicalParameterId { get; set; }

    /// <summary>
    /// Технологический параметр
    /// </summary>
    public virtual TParameter TechnologicalParameter { get; set; } = null!;

    /// <summary>
    /// Сырое ("грязное") значение, переданное от SCADA/IoT или оператора
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Точное время фиксации параметра (Time-Series метка)
    /// </summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;
}