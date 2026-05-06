using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public class EntityCancelEventArgs<T>(
    T entity,
    EntityStateChangeEnum state,
    List<PropertyChangeInfo> changes,
    DbContext context)
    : EntityCancelArgsBase(entity!, state, changes, context) where T : class
{
    public new T Entity => (T)base.Entity;
}