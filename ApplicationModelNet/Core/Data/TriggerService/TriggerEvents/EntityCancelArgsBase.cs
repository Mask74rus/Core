using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public class EntityCancelArgsBase(
    object entity,
    EntityStateChangeEnum state,
    List<PropertyChangeInfo> changes,
    DbContext context) : EntityEventArgsBase(entity, state, changes)
{
    public DbContext Context { get; } = context;
    public bool Cancel { get; set; }
    public string? ErrorMessage { get; set; }
}