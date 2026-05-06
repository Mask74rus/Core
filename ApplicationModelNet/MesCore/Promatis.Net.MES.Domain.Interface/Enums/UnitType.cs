namespace Promatis.Net.MES.Domain.Interface;

[Flags]
public enum UnitType
{
    None = 0,

    // --- Структурные подразделения (Organizational) ---
    Workshop = 1,       // Цех
    Section = 2,        // Участок
    Line = 4,           // Линия / Конвейер
    Workstation = 8,    // Рабочее место / Пост

    // --- Складская логистика (Storage & Logistics) ---
    Storage = 16,        // Склад
    Zone = 32,           // Зона хранения (например, зона приемки или зона А)
    Rack = 64,           // Стеллаж
    Cell = 128,           // Ячейка (конечный адрес хранения)
    Crane = 256,          // Кран / Подъемное устройство

    // --- Оборудование и техника (Equipment) ---
    MachineTool = 512,    // Станок
    Table = 1024,          // Стол / Верстак
    Vehicle = 2048,        // Транспортное средство (погрузчик, тягач)
    Conveyor = 4096,       // Автономный транспортер

    // --- Прочее ---
    Other  = 8192         // Прочее
}