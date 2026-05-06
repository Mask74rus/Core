namespace Promatis.Net.Data;

// Базовый класс для внутреннего использования в сервисе
public class EntityChangedArgsBase(
    object entity,
    EntityStateChangeEnum state,
    List<PropertyChangeInfo> changes,
    string? changedBy,
    DateTime changedAt) : EntityEventArgsBase(entity, state, changes) // <-- Наследуем здесь
{
    public string? ChangedBy { get; } = changedBy;
    public DateTime ChangedAt { get; } = changedAt;
}