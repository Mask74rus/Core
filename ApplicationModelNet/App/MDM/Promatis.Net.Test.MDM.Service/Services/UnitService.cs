using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;
using Promatis.Net.Test.MDM.Domain;

namespace Promatis.Net.Test.MDM.Service;

/// <summary>
/// Конечный рабочий сервис управления структурой предприятия на самом низком уровне.
/// Наследует всю готовую математику дерева СУБД и реализует фабрику полиморфных шаблонов.
/// </summary>
public class UnitService(IDbContextFactory<MdmApplicationDbContext> contextFactory)
    : UnitBaseService<MdmApplicationDbContext>(contextFactory)
{
    /// <summary>
    /// Конечная прикладная фабрика полиморфизма.
    /// </summary>
    public override Task<UnitBase> CreateChildTemplateAsync(UnitBase parent)
    {
        UnitKind childKind = parent.Kind switch
        {
            UnitKind.Department => UnitKind.Production,
            UnitKind.Production => UnitKind.Position,
            UnitKind.Storage => UnitKind.Storage,
            UnitKind.Transport => UnitKind.Position,
            _ => UnitKind.Position
        };

        UnitBase childUnit = childKind switch
        {
            UnitKind.Department => new DepartmentUnit { Type = UnitType.Other, ParentId = parent.Id },
            UnitKind.Production => new ProductionUnit { Type = UnitType.Other, ParentId = parent.Id },
            UnitKind.Storage => new StorageUnit { Type = UnitType.Other, ParentId = parent.Id },
            UnitKind.Transport => new TransportUnit { Type = UnitType.Other, ParentId = parent.Id },
            UnitKind.Position => new PositionUnit { Type = UnitType.Other, ParentId = parent.Id },
            _ => throw new ArgumentOutOfRangeException(nameof(childKind))
        };

        childUnit.Parent = null;
        return Task.FromResult(childUnit);
    }
}