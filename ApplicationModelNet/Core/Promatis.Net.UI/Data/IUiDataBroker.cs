using Promatis.Net.Data;

namespace Promatis.Net.UI;

/// <summary>
/// Контракт брокера данных, управляющего выбором источника (Server/OZU) и стратегией отображения.
/// Не содержит дефолтных режимов — программист обязан явно сконфигурировать поведение формы.
/// </summary>
public interface IUiDataBroker<TEntity, TQueryState, TResultData> : IDisposable where TEntity : class
{
    /// <summary>
    /// Признак, указывающий, что текущий инстанс брокера на форме работает в режиме оперативной памяти.
    /// </summary>
    bool IsInMemoryMode { get; }

    /// <summary>
    /// Жесткая конфигурация работы напрямую с СУБД / gRPC API (Server Mode).
    /// </summary>
    void ConfigureServerMode(Func<TQueryState, CancellationToken, Task<TResultData>> serverDataProvider);

    /// <summary>
    /// Жесткая конфигурация работы через изолированное локальное ОЗУ-хранилище (InMemory Mode).
    /// </summary>
    void ConfigureInMemoryMode(
        IUiOzuCache<TEntity> ozuCache,
        Func<TQueryState, CancellationToken, Task<List<TEntity>>> serverDataLoader, // Передаем знание О ТОМ, КАК загрузить
        Func<TQueryState, IReadOnlyList<TEntity>, TResultData> inMemoryEvaluator // Работает с IReadOnlyList
    );

    /// <summary>
    /// Универсальная точка асинхронного запроса данных визуализаторами интерфейса (GridPage / TreePage).
    /// Если ни один режим не был сконфигурирован программистом, вызовет InvalidOperationException.
    /// </summary>
    Task<TResultData> FetchDataAsync(TQueryState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Точка входа для проброса глобального сигнала OnEntityCommitted в локальный кэш формы.
    /// </summary>
    void HandleDatabaseCommit(EntityStateChangeEnum state, object entity);
}