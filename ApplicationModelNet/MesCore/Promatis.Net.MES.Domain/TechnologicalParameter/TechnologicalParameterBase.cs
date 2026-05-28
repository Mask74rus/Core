using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый технологический параметр (справочник характеристик процессов).
/// </summary>
public abstract class TechnologicalParameterBase : ReferenceBase, ITechnologicalParameter
{
    /// <summary>
    /// Внешний ключ на справочник единиц измерения.
    /// </summary>
    public Guid? UnitOfMeasurementId { get; set; }

    public virtual UnitOfMeasurement? UnitOfMeasurement { get; set; }

    /// <summary>
    /// Тип данных параметра (например: Число, Строка, Булево).
    /// </summary>
    public string DataType { get; set; } = "Numeric";

    /// <summary>
    /// Разрешенные методы рассчёта
    /// </summary>
    public CalculationMethod AllowedMethods { get; set; } = CalculationMethod.None;
}