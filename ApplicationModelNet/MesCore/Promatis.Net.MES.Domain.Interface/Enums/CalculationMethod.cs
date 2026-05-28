using System.ComponentModel;

namespace Promatis.Net.MES.Domain.Interface;

/// <summary>
/// Методы расчета тех. параметров
/// </summary>
[Flags]
public enum CalculationMethod
{
    [Description("Не задан")]
    None = 0,               // 00000000

    [Description("Максимальное значение (MAX)")]
    Max = 1,

    [Description("Минимальное значение (MIN)")]
    Min = 2,

    [Description("Среднее значение (AVG)")]
    Avg = 4,

    [Description("Первое значение (FIRST)")]
    First = 8,

    [Description("Последнее значение (LAST)")]
    Last = 16,

    [Description("Все значения (ALL)")]
    All = 32
}