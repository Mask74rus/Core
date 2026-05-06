using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public interface IDatabaseTriggerService
{
    // Метод для настройки связей "Сущность-Триггер"
    void Register<TEntity, TTrigger>()
        where TEntity : class
        where TTrigger : class;

    // Методы выполнения (вызываются интерцептором)
    Task ValidateAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, DbContext context);
    Task NotifyAsync(object entity, EntityStateChangeEnum state, List<PropertyChangeInfo> changes, string? user, DateTime at);
}