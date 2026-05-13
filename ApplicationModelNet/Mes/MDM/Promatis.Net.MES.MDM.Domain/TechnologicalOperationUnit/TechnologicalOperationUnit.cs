using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.MDM.Domain;

public class TechnologicalOperationUnit : TechnologicalOperationUnitBase
{
    /// <summary>
    /// Перекрываем базовое свойство СУБД строго типизированным свойством для бизнес-логики.
    /// </summary>
    public new virtual TechnologicalOperation Operation
    {
        get => base.Operation as TechnologicalOperation ?? null!;
        set => base.Operation = value;
    }
}