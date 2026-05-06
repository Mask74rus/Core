using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для всех организационных и производственных единиц.
/// </summary>
public abstract class UnitBase : ReferenceTreeBase<UnitBase>
{
    /// <summary>
    /// Категория юнита. Задается при создании конкретного наследника.
    /// </summary>
    public UnitKind Kind { get; init; }

    /// <summary>
    /// Конкретный тип юнита.
    /// </summary>
    public required UnitType Type { get; set; }
}