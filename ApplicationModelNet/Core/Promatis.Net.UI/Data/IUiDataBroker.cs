using Promatis.Net.Data;

namespace Promatis.Net.UI;

/// <summary>
/// Универсальный интерфейс управляющего автомата данных (Брокера).
/// Задает жесткие контракты для серверного и оперативного режимов работы.
/// </summary>
public interface IUiDataBroker<TEntity, TQueryState, TResultData> : IDisposable
    where TEntity : class
{
    /// <summary>
    /// Жесткая конфигурация работы напрямую с СУБД / gRPC API (Server Mode).
    /// </summary>
    void ConfigureServerMode(Func<TQueryState, CancellationToken, Task<TResultData>> serverDataProvider);

    /// <summary>
    /// Жесткая конфигурация работы через изолированное локальное ОЗУ-хранилище (InMemory Mode).
    /// </summary>
    void ConfigureInMemoryMode(
        IUiOzuCache<TEntity> ozuCache,
        Func<CancellationToken, Task<List<TEntity>>> serverDataLoader, // ХИРУРГИЧЕСКИ ИСПРАВЛЕНО: Убран TQueryState для честной выкачки всей таблицы
        Func<IReadOnlyList<TEntity>, TQueryState, TResultData> inMemoryEvaluator // Работает с IReadOnlyList и текущим стейтом MudBlazor
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