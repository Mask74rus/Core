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
    /// Прямой доступ к текущему массиву данных только для чтения. 
    /// Ни одна форма больше не может перезаписать или очистить этот список вручную.
    /// </summary>
    IReadOnlyList<TEntity> InMemoryItems { get; }

    /// <summary>
    /// Точка первоначального наполнения кэша данными, полученными от формы.
    /// Вызывается строго один раз при инициализации.
    /// </summary>
    void Initialize(List<TEntity> initialItems);

    /// <summary>
    /// Автономный ОЗУ-движок мутаций. Хирургически точно обновляет коллекцию в памяти.
    /// </summary>
    void ApplyOzuDelta(EntityStateChangeEnum state, TEntity entity);

    void SetMutationStrategy(IOzuMutationStrategy<TEntity> strategy);

    /// <summary>
    /// Выполняет произвольную операцию чтения/фильтрации над кэшем внутри потокобезопасного периметра.
    /// Перехватывает поток UI и защищает его от параллельных мутаций из фоновых потоков СУБД.
    /// </summary>
    TResult ExecuteInLock<TResult>(Func<IReadOnlyList<TEntity>, TResult> evaluator);
}