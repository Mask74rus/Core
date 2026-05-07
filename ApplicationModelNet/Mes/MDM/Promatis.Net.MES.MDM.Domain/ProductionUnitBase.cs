using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.MDM.Domain;

public abstract class ProductionUnitBase : UnitBase
{
    protected ProductionUnitBase() => Kind = UnitKind.Production;
}