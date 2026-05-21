namespace Promatis.Net.MES.Domain.Interface;

public enum CalculationMethod
{
    /// <summary>
    /// Значение по умолчанию. Означает не выбрано
    /// </summary>
    None = -1,

    /// <summary>
    /// Максимальное значение
    /// </summary>
    Max = 0,

    /// <summary>
    /// Минимальное значение
    /// </summary>
    Min = 1,

    /// <summary>
    /// Среднее значение
    /// </summary>
    Avg = 2,

    /// <summary>
    /// Первое значение
    /// </summary>
    First = 3,

    /// <summary>
    /// Последнее значение
    /// </summary>
    Last = 4,

    /// <summary>
    /// Все значения
    /// </summary>
    All = 5
}