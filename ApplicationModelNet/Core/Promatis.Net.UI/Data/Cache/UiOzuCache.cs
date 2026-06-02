using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI;

/// <summary>
/// Реализация изолированного кэша в оперативной памяти для конкретного инстанса формы.
/// </summary>
public class UiOzuCache<TEntity> : IUiOzuCache<TEntity> where TEntity : class
{
    private IOzuMutationStrategy<TEntity> _mutationStrategy = new FlatOzuMutationStrategy<TEntity>();

    public List<TEntity> InMemoryItems { get; set; } = new();

    public UiOzuCache() { }

    public UiOzuCache(List<TEntity> initialItems)
    {
        InMemoryItems = initialItems ?? throw new ArgumentNullException(nameof(initialItems));
    }

    // Метод для динамической смены стратегии (вызывается контекстом дерева)
    public void SetMutationStrategy(IOzuMutationStrategy<TEntity> strategy)
    {
        _mutationStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public void ApplyOzuDelta(EntityStateChangeEnum state, TEntity entity)
    {
        // Делегируем работу выбранной стратегии мутации
        _mutationStrategy.ApplyDelta(InMemoryItems, state, entity, GetEntityId);
    }

    private object? GetEntityId(TEntity item)
    {
        Type? hasKeyInterface = item.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainObjectHasKey<>));

        if (hasKeyInterface != null)
        {
            return hasKeyInterface.GetProperty("Id")?.GetValue(item);
        }

        return item.GetType().GetProperty("Id")?.GetValue(item);
    }
}