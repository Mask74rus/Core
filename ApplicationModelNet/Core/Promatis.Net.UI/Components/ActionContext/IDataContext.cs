namespace Promatis.Net.UI.Components;

/// <summary>
/// Интерфейс слоя данных, расширяющий базовую разметку холста gRPC-транспортом и событийной моделью.
/// </summary>
public interface IDataContext : IWorkspaceContext
{
    /// <summary>
    /// Флаг, указывающий, активирован ли gRPC-транспорт для работы с сервером.
    /// </summary>
    bool IsTransportActivated { get; }

    /// <summary>
    /// Асинхронная активация брокеров и транспорта данных.
    /// </summary>
    Task ActivateTransportAsync();

    /// <summary>
    /// Событие, срабатывающее при любом изменении внутреннего стейта контекста.
    /// </summary>
    event Action? OnContextUpdated;

    /// <summary>
    /// Принудительное уведомление всех подписчиков об изменении состояния.
    /// </summary>
    void NotifyContextUpdated();
}