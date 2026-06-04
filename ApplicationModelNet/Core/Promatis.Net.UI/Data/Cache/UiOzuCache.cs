using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI;

/// <summary>
/// Реализация изолированного кэша в оперативной памяти для конкретного инстанса формы.
/// </summary>
public class UiOzuCache<TEntity> : IUiOzuCache<TEntity> where TEntity : class
{
    private IOzuMutationStrategy<TEntity> _mutationStrategy = new FlatOzuMutationStrategy<TEntity>();
    private readonly List<TEntity> _inMemoryItems = [];

    // Единый объект синхронизации для потоков чтения и записи
    private readonly Lock _lockObject = new();

    public IReadOnlyList<TEntity> InMemoryItems => _inMemoryItems;

    public UiOzuCache() { }

    public UiOzuCache(List<TEntity> initialItems)
    {
        Initialize(initialItems);
    }

    public void Initialize(List<TEntity> initialItems)
    {
        if (initialItems == null) throw new ArgumentNullException(nameof(initialItems));

        lock (_lockObject)
        {
            _inMemoryItems.Clear();
            _inMemoryItems.AddRange(initialItems);
        }
    }

    public void SetMutationStrategy(IOzuMutationStrategy<TEntity> strategy)
    {
        _mutationStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public void ApplyOzuDelta(EntityStateChangeEnum state, TEntity entity)
    {
        // Поток СУБД монопольно захватывает замок на время мутации
        lock (_lockObject)
        {
            _mutationStrategy.ApplyDelta(_inMemoryItems, state, entity, GetEntityId);
        }
    }

    public TResult ExecuteInLock<TResult>(Func<IReadOnlyList<TEntity>, TResult> evaluator)
    {
        if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));

        // Поток UI (Blazor) монопольно захватывает замок на время фильтрации/вычисления данных экрана
        lock (_lockObject)
        {
            return evaluator(_inMemoryItems);
        }
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