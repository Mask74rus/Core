namespace Promatis.Net.Domain;

/// <summary>
/// Базовый класс для всех справочников содержащих деревья
/// </summary>
public abstract class ReferenceTreeBase : ReferenceBase
{
    public Guid? ParentId { get; set; }
}