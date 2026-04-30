namespace Promatis.Net.Data;

public class EntityChangedEventArgs<T>(T entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? changedBy, DateTime changedAt) : EventArgs
{
    public T Entity { get; } = entity;
    public EntityStateChangeEnum State { get; } = state;
    public List<PropertyChangeInfo> Changes { get; } = changes;
    public string? ChangedBy { get; } = changedBy;
    public DateTime ChangedAt { get; } = changedAt;
    public bool Handled { get; set; }
}