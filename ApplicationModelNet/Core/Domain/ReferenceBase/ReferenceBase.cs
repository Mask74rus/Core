using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Domain;

/// <summary>
/// Базовый класс для всех справочников
/// </summary>
public abstract class ReferenceBase : DomainObject, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Code { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}