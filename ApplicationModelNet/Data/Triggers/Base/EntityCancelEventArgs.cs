using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public class EntityCancelEventArgs<T>(
    T entity,
    EntityStateChangeEnum state,
    List<PropertyChangeInfo> changes,
    DbContext context)
    : EventArgs
{
    public T Entity { get; } = entity;
    public EntityStateChangeEnum State { get; } = state;
    public List<PropertyChangeInfo> Changes { get; } = changes;
    public DbContext Context { get; } = context;
    public bool Cancel { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Handled { get; set; }
}