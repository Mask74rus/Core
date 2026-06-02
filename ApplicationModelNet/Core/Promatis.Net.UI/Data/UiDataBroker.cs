using Promatis.Net.Data;

namespace Promatis.Net.UI;

/// <summary>
/// Платформенный брокер данных. Централизованно управляет поставкой данных для визуализаторов.
/// Полностью абстрагирует UI от конкретных интерфейсов доменных служб бэкенда.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности</typeparam>
public class UiDataBroker<TEntity, TQueryState, TResultData> : IUiDataBroker<TEntity, TQueryState, TResultData>
    where TEntity : class
{
    private Func<TQueryState, CancellationToken, Task<TResultData>>? _serverDataProvider;
    private Func<TQueryState, List<TEntity>, TResultData>? _inMemoryEvaluator;
    private IUiOzuCache<TEntity>? _ozuCache;
    private readonly Action? _onDataChangedNotifier; // Ссылка на NotifyStateChanged контекста

    private bool _isInitialized;
    private bool _isInMemoryMode;

    public bool IsInMemoryMode => _isInMemoryMode;

    // Внедряем notifier в конструктор для реактивного пинка UI
    public UiDataBroker(Action? onDataChangedNotifier = null)
    {
        _onDataChangedNotifier = onDataChangedNotifier;

        // ГЛОБАЛЬНЫЙ ПЕРЕХВАТ: Каждый брокер в системе нативно начинает слушать СУБД
        DatabaseTriggerService.OnEntityCommitted += HandleGlobalEntityCommitted;
    }

    private void HandleGlobalEntityCommitted(EntityStateChangeEnum state, object entity)
    {
        // Вызываем наш внутренний метод обработки мутаций
        HandleDatabaseCommit(state, entity);
    }

    public void ConfigureServerMode(Func<TQueryState, CancellationToken, Task<TResultData>> serverDataProvider)
    {
        _serverDataProvider = serverDataProvider ?? throw new ArgumentNullException(nameof(serverDataProvider));
        _inMemoryEvaluator = null;
        _ozuCache = null;
        _isInMemoryMode = false;
        _isInitialized = true;
    }

    public void ConfigureInMemoryMode(IUiOzuCache<TEntity> ozuCache, Func<TQueryState, List<TEntity>, TResultData> inMemoryEvaluator)
    {
        _ozuCache = ozuCache ?? throw new ArgumentNullException(nameof(ozuCache));
        _inMemoryEvaluator = inMemoryEvaluator ?? throw new ArgumentNullException(nameof(inMemoryEvaluator));
        _serverDataProvider = null;
        _isInMemoryMode = true;
        _isInitialized = true;
    }

    public async Task<TResultData> FetchDataAsync(TQueryState state, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                $"Брокер данных для '{typeof(TEntity).Name}' не был сконфигурирован. " +
                $"Вы обязаны вызвать ConfigureServerMode или ConfigureInMemoryMode в конструкторе контекста формы.");
        }

        if (_serverDataProvider != null)
        {
            return await _serverDataProvider(state, cancellationToken);
        }

        if (_isInMemoryMode && _ozuCache != null && _inMemoryEvaluator != null)
        {
            return _inMemoryEvaluator(state, _ozuCache.InMemoryItems);
        }

        throw new InvalidOperationException("Критический сбой внутренней конфигурации брокера данных.");
    }

    public void HandleDatabaseCommit(EntityStateChangeEnum state, object entity)
    {
        // Проверяем: относится ли прилетевший из СУБД объект к типу данных текущего экрана?
        if (entity is not TEntity typedEntity) return;

        // МОДИФИЦИРОВАНО: Если мы в режиме In-Memory, точечно обновляем локальный кэш ОЗУ
        if (_isInMemoryMode && _ozuCache != null)
        {
            _ozuCache.ApplyOzuDelta(state, typedEntity);
        }

        // В ОБОИХ РЕЖИМАХ (И Server-Side, и In-Memory):
        // Даем реактивный импульс на уровень UI-контекста, чтобы MudDataGrid плавно обновил строки на экране!
        _onDataChangedNotifier?.Invoke();
    }

    public void Dispose()
    {
        // КРИТИЧЕСКИ ВАЖНО: Отписываемся от статического эвента службы триггеров при закрытии брокера,
        // полностью исключая утечки оперативной памяти в Blazor
        DatabaseTriggerService.OnEntityCommitted -= HandleGlobalEntityCommitted;
    }
}