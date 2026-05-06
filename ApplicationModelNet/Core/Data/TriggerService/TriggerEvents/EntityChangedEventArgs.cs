namespace Promatis.Net.Data;

// Ваш типизированный класс для триггеров
public class EntityChangedEventArgs<T>(
    T entity, EntityStateChangeEnum state, 
    List<PropertyChangeInfo> changes, 
    string? changedBy, 
    DateTime changedAt)
    : EntityChangedArgsBase(entity!, state, changes, changedBy, changedAt) where T : class
{
    public new T Entity => (T)base.Entity;
}