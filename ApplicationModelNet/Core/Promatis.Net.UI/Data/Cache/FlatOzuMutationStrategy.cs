using Promatis.Net.Data;

namespace Promatis.Net.UI;

public class FlatOzuMutationStrategy<TEntity> : IOzuMutationStrategy<TEntity> where TEntity : class
{
    public void ApplyDelta(List<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector)
    {
        object? entityId = idSelector(entity);
        if (entityId == null) return;

        switch (state)
        {
            case EntityStateChangeEnum.Added:
                if (!inMemoryItems.Any(x => Equals(idSelector(x), entityId)))
                {
                    inMemoryItems.Add(entity);
                }
                break;

            case EntityStateChangeEnum.Modified:
                int index = inMemoryItems.FindIndex(x => Equals(idSelector(x), entityId));
                if (index >= 0) inMemoryItems[index] = entity;
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                TEntity? itemToRemove = inMemoryItems.FirstOrDefault(x => Equals(idSelector(x), entityId));
                if (itemToRemove != null) inMemoryItems.Remove(itemToRemove);
                break;
        }
    }
}