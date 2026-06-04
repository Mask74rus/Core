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
    private Func<TQueryState, CancellationToken, Task<List<TEntity>>>? _serverDataLoader;
    private Func<TQueryState, IReadOnlyList<TEntity>, TResultData>? _inMemoryEvaluator;
    private IUiOzuCache<TEntity>? _ozuCache;
    private readonly Action? _onDataChangedNotifier;

    private bool _isInitialized;
    private bool _isInMemoryMode;
    private bool _isOzuLoaded;

    // Инфраструктура для дебаунса сетевых уведомлений в ServerMode
    private CancellationTokenSource? _debounceCts;
    private readonly object _debounceLock = new(); // Замок для потокобезопасного сброса таймера
    private const int DebounceDelayMs = 300; // Временное окно ожидания затишья в БД (300 мс)

    public bool IsInMemoryMode => _isInMemoryMode;

    public UiDataBroker(Action? onDataChangedNotifier = null)
    {
        _onDataChangedNotifier = onDataChangedNotifier;
        DatabaseTriggerService.OnEntityCommitted += HandleGlobalEntityCommitted;
    }

    private void HandleGlobalEntityCommitted(EntityStateChangeEnum state, object entity)
    {
        HandleDatabaseCommit(state, entity);
    }

    public void ConfigureServerMode(Func<TQueryState, CancellationToken, Task<TResultData>> serverDataProvider)
    {
        _serverDataProvider = serverDataProvider ?? throw new ArgumentNullException(nameof(serverDataProvider));
        _serverDataLoader = null;
        _inMemoryEvaluator = null;
        _ozuCache = null;
        _isInMemoryMode = false;
        _isInitialized = true;
        _isOzuLoaded = false;
    }

    public void ConfigureInMemoryMode(
        IUiOzuCache<TEntity> ozuCache,
        Func<TQueryState, CancellationToken, Task<List<TEntity>>> serverDataLoader,
        Func<TQueryState, IReadOnlyList<TEntity>, TResultData> inMemoryEvaluator)
    {
        _ozuCache = ozuCache ?? throw new ArgumentNullException(nameof(ozuCache));
        _serverDataLoader = serverDataLoader ?? throw new ArgumentNullException(nameof(serverDataLoader));
        _inMemoryEvaluator = inMemoryEvaluator ?? throw new ArgumentNullException(nameof(inMemoryEvaluator));
        _serverDataProvider = null;
        _isInMemoryMode = true;
        _isInitialized = true;
        _isOzuLoaded = false;
    }

    public async Task<TResultData> FetchDataAsync(TQueryState state, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException($"Брокер данных для '{typeof(TEntity).Name}' не был сконфигурирован.");
        }

        if (_serverDataProvider != null)
        {
            return await _serverDataProvider(state, cancellationToken);
        }

        if (_isInMemoryMode && _ozuCache != null && _inMemoryEvaluator != null && _serverDataLoader != null)
        {
            if (!_isOzuLoaded)
            {
                List<TEntity> initialData = await _serverDataLoader(state, cancellationToken);
                _ozuCache.Initialize(initialData);
                _isOzuLoaded = true;
            }

            return _ozuCache.ExecuteInLock(items => _inMemoryEvaluator(state, items));
        }

        throw new InvalidOperationException("Критический сбой внутренней конфигурации брокера данных.");
    }

    public void HandleDatabaseCommit(EntityStateChangeEnum state, object entity)
    {
        // Проверяем: относится ли прилетевший из СУБД объект к типу данных текущего экрана?
        if (entity is not TEntity typedEntity) return;

        if (_isInMemoryMode)
        {
            // РЕЖИМ IN-MEMORY: Мутируем ОЗУ мгновенно без задержек для сохранения целостности кэша
            if (_ozuCache != null && _isOzuLoaded)
            {
                _ozuCache.ApplyOzuDelta(state, typedEntity);
            }

            // Сразу пинаем UI, так как сетевого запроса не будет — вычисления пройдут мгновенно в ОЗУ
            _onDataChangedNotifier?.Invoke();
        }
        else
        {
            // РЕЖИМ SERVER-MODE: Включаем дебаунс для предотвращения сетевого шторма gRPC/API запросов
            lock (_debounceLock)
            {
                // Если предыдущий таймер еще тикал (пачка изменений продолжается) — отменяем его
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();

                // Создаем новый токен задержки
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                // Запускаем фоновое ожидание затишья в БД
                Task.Delay(DebounceDelayMs, token).ContinueWith(task =>
                {
                    // Вызываем пинок UI только если задача успешно завершилась и не была отменена новой дельтой
                    if (task.Status == TaskStatus.RanToCompletion && !token.IsCancellationRequested)
                    {
                        _onDataChangedNotifier?.Invoke();
                    }
                }, TaskScheduler.Default);
            }
        }
    }

    public void Dispose()
    {
        // Отписываемся от статического эвента службы триггеров СУБД
        DatabaseTriggerService.OnEntityCommitted -= HandleGlobalEntityCommitted;

        // Зачищаем инфраструктуру дебаунса при уничтожении брокера формой
        lock (_debounceLock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;
        }
    }
}