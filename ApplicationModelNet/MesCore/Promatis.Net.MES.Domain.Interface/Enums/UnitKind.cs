using System.ComponentModel;

namespace Promatis.Net.MES.Domain.Interface;

public enum UnitKind
{
    [Description("Складская логистика")]
    Storage = UnitType.Storage | UnitType.Zone | UnitType.Rack | UnitType.Cell | UnitType.Crane,

    [Description("Производственная зона")]
    Production = UnitType.Workshop | UnitType.Section | UnitType.Line | UnitType.Workstation | UnitType.MachineTool | UnitType.Table,

    [Description("Транспортный узел")]
    Transport = UnitType.Vehicle | UnitType.Conveyor,

    [Description("Подразделение")]
    Department = UnitType.Workshop | UnitType.Section | UnitType.Other,

    [Description("Рабочая точка / Ячейка")]
    Position = UnitType.Cell | UnitType.Other
}