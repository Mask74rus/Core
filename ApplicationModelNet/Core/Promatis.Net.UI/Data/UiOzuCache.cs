using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI;

/// <summary>
/// Реализация изолированного кэша в оперативной памяти для конкретного инстанса формы.
/// </summary>
public class UiOzuCache<TEntity> : IUiOzuCache<TEntity> where TEntity : class
{
    public List<TEntity> InMemoryItems { get; set; } = new();

    public UiOzuCache() { }

    public UiOzuCache(List<TEntity> initialItems)
    {
        InMemoryItems = initialItems ?? throw new ArgumentNullException(nameof(initialItems));
    }

    /// <summary>
    /// Высокопроизводительный ОЗУ-движок мутаций. Безопасно обновляет локальную память.
    /// </summary>
    public void ApplyOzuDelta(EntityStateChangeEnum state, TEntity entity)
    {
        object? entityId = GetEntityId(entity);
        if (entityId == null) return;

        switch (state)
        {
            case EntityStateChangeEnum.Added:
                // Защита от дублирования строк при асинхронных сетевых всплесках
                if (!InMemoryItems.Any(x => Equals(GetEntityId(x), entityId)))
                {
                    InMemoryItems.Add(entity);
                }
                break;

            case EntityStateChangeEnum.Modified:
                int index = InMemoryItems.FindIndex(x => Equals(GetEntityId(x), entityId));
                if (index >= 0)
                {
                    // Подменяем старый объект на новый актуальный слепок из СУБД целиком
                    InMemoryItems[index] = entity;
                }
                break;

            case EntityStateChangeEnum.Deleted:
            case EntityStateChangeEnum.SoftDeleted:
                TEntity? itemToRemove = InMemoryItems.FirstOrDefault(x => Equals(GetEntityId(x), entityId));
                if (itemToRemove != null)
                {
                    InMemoryItems.Remove(itemToRemove);
                }
                break;
        }
    }

    /// <summary>
    /// Извлечение ID на основе реальных доменных интерфейсов платформы.
    /// </summary>
    private object? GetEntityId(TEntity item)
    {
        var hasKeyInterface = item.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainObjectHasKey<>));

        if (hasKeyInterface != null)
        {
            return hasKeyInterface.GetProperty("Id")?.GetValue(item);
        }

        return item.GetType().GetProperty("Id")?.GetValue(item);
    }
}