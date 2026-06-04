using Promatis.Net.Data;

namespace Promatis.Net.UI;

public class FlatOzuMutationStrategy<TEntity> : IOzuMutationStrategy<TEntity> where TEntity : class
{
    // Теперь контракт принимает гибкую ICollection вместо жесткого List
    public void ApplyDelta(ICollection<TEntity> inMemoryItems, EntityStateChangeEnum state, TEntity entity, Func<TEntity, object?> idSelector)
    {
        object? entityId = idSelector(entity);
        if (entityId == null) return;

        switch (state)
        {
            case EntityStateChangeEnum.Added:
                // Добавляем, только если элемента с таким Id еще нет в коллекции
                if (!inMemoryItems.Any(x => Equals(idSelector(x), entityId)))
                {
                    inMemoryItems.Add(entity);
                }
                break;

            case EntityStateChangeEnum.Modified:
                // ИСПРАВЛЕНО: Так как ICollection не поддерживает обращение по индексу [idx], 
                // мы используем универсальный и безопасный для MudBlazor подход «удалил старую ссылку -> добавил новую»
                TEntity? existingItem = inMemoryItems.FirstOrDefault(x => Equals(idSelector(x), entityId));
                if (existingItem != null)
                {
                    inMemoryItems.Remove(existingItem);
                }
                inMemoryItems.Add(entity);
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                TEntity? itemToRemove = inMemoryItems.FirstOrDefault(x => Equals(idSelector(x), entityId));
                if (itemToRemove != null)
                {
                    inMemoryItems.Remove(itemToRemove);
                }
                break;
        }
    }
}