using Promatis.Net.Data;

namespace Promatis.Net.UI;

public interface IOzuMutationStrategy<TEntity> where TEntity : class
{
    /// <summary>
    /// Применяет дельту изменений СУБД к изменяемой коллекции графа объектов ОЗУ.
    /// </summary>
    void ApplyDelta(ICollection<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector);
}