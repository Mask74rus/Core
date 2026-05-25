using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для всех организационных и производственных единиц.
/// </summary>
public abstract class UnitBase : ReferenceTreeBase<UnitBase>, IAudit
{
    public UnitKind Kind { get; init; }
    public required UnitType Type { get; set; }
}