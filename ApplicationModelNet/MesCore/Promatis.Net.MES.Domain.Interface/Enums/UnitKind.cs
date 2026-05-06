namespace Promatis.Net.MES.Domain.Interface;

public enum UnitKind
{
    // Явно указываем, из чего состоит каждая категория
    Storage = UnitType.Storage | UnitType.Zone | UnitType.Rack | UnitType.Cell | UnitType.Crane,
    Production = UnitType.Workshop | UnitType.Section | UnitType.Line | UnitType.Workstation | UnitType.MachineTool | UnitType.Table,
    Transport = UnitType.Vehicle | UnitType.Conveyor,

    // Департамент может содержать крупные узлы
    Department = UnitType.Workshop | UnitType.Section | UnitType.Other,

    // Позиция — конкретные рабочие точки
    Position = UnitType.Cell | UnitType.Other
}