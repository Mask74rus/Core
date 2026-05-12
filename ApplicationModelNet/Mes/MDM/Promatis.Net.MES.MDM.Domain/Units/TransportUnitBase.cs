using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.MDM.Domain;

public abstract class TransportUnitBase : UnitBase
{
    protected TransportUnitBase() => Kind = UnitKind.Transport;
}