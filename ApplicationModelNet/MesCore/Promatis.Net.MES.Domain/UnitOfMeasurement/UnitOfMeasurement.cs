using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Доменная сущность единицы измерения (НСИ) для технологических параметров платформы.
/// </summary>
public class UnitOfMeasurement : ReferenceBase
{
    // Свойства Id, Code, Name, Description нативно унаследованы от ReferenceBase.
    // Code — краткое обозначение (например, "°C", "мм", "об/мин").
    // Name — полное наименование (например, "Градус Цельсия", "Миллиметр").
}