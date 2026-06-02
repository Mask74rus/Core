using Promatis.Net.Data;

namespace Promatis.Net.UI;

public interface IOzuMutationStrategy<TEntity> where TEntity : class
{
    void ApplyDelta(List<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector);
}