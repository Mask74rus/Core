namespace Promatis.Net.Domain;

/// <summary>
/// Аудит системы
/// </summary>
public class AuditLog : DomainObject
{
    /// <summary>
    /// Наименование события
    /// </summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Действие
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Дата изменения
    /// </summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>
    /// Изменено
    /// </summary>
    public string? ChangedBy { get; init; }

    /// <summary>
    /// Строка изменений
    /// </summary>
    public string ChangesJson { get; init; } = string.Empty;
}