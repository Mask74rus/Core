using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.MDM.Domain;

public abstract class PositionUnitBase : UnitBase
{
    protected PositionUnitBase() => Kind = UnitKind.Position;
}