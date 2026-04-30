namespace Promatis.Net.Domain;

public class AuditLog : DomainObject
{
    public string EntityName { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Action { get; init; } = string.Empty;

    public DateTime ChangedAt { get; init; }
    public string? ChangedBy { get; init; }

    public string ChangesJson { get; init; } = string.Empty;
}