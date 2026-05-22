using System.ComponentModel;

namespace Promatis.Net.MES.Domain.Interface;

[Flags]
public enum UnitType
{
    None = 0,

    [Description("Цех")]
    Workshop = 1,

    [Description("Участок")]
    Section = 2,

    [Description("Линия / Конвейер")]
    Line = 4,

    [Description("Рабочее место")]
    Workstation = 8,

    [Description("Склад")]
    Storage = 16,

    [Description("Зона хранения")]
    Zone = 32,

    [Description("Стеллаж")]
    Rack = 64,

    [Description("Ячейка адреса хранения")]
    Cell = 128,

    [Description("Кран / Подъемник")]
    Crane = 256,

    [Description("Станок")]
    MachineTool = 512,

    [Description("Верстак / Стол")]
    Table = 1024,

    [Description("Транспортное средство")]
    Vehicle = 2048,

    [Description("Автономный транспортер")]
    Conveyor = 4096,

    [Description("Прочее")]
    Other = 8192
}