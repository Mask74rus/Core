namespace Promatis.Net.Data;

// Для правил "ПОСЛЕ" сохранения (уведомления/шина)
public interface IAfterSaveTrigger<T> where T : class
{
    Task HandleAsync(EntityChangedEventArgs<T> args);
}