using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public interface IDatabaseTriggerService
{
    Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context);
    Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at);
}