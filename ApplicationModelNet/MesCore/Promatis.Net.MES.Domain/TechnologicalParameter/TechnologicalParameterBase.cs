using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый технологический параметр (справочник характеристик процессов).
/// </summary>
public abstract class TechnologicalParameterBase : ReferenceBase, ITechnologicalParameter
{
    /// <summary>
    /// Единица измерения параметра (например: мм, об/мин, °C).
    /// </summary>
    public string UnitOfMeasurement { get; set; } = string.Empty;

    /// <summary>
    /// Тип данных параметра (например: Число, Строка, Булево).
    /// </summary>
    public string DataType { get; set; } = "Numeric";
}