using Promatis.Net.Data;

namespace Promatis.Net.UI;

/// <summary>
/// Контракт изолированного локального ОЗУ-хранилища данных конкретной экранной формы.
/// Использует системный EntityStateChangeEnum для применения точечных дельт изменений.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности.</typeparam>
public interface IUiOzuCache<TEntity> where TEntity : class
{
    /// <summary>
    /// Прямой доступ к текущему массиву данных, хранящемуся в оперативной памяти конкретной формы.
    /// </summary>
    List<TEntity> InMemoryItems { get; set; }

    /// <summary>
    /// Автономный ОЗУ-движок мутаций. Хирургически точно обновляет коллекцию в памяти,
    /// принимая системный стейт транзакции СУБД.
    /// </summary>
    void ApplyOzuDelta(EntityStateChangeEnum state, TEntity entity);

    void SetMutationStrategy(IOzuMutationStrategy<TEntity> strategy);
}