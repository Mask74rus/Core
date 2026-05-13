using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый технологический параметр (справочник характеристик процессов).
/// </summary>
public abstract class TechnologicalParameterBase : ReferenceBase
{
    /// <summary>
    /// Единица измерения параметра (например: мм, об/мин, °C).
    /// </summary>
    public string UnitOfMeasurement { get; set; } = string.Empty;

    /// <summary>
    /// Тип данных параметра (например: Число, Строка, Булево) для валидации значений на верхних слоях.
    /// </summary>
    public string DataType { get; set; } = "Numeric";
}