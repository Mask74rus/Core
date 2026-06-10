using Promatis.Net.Data;

namespace Promatis.Net.UI;

/// <summary>
/// Промышленный автомат управления gRPC-транспортом, Ozu-кэшем и реактивными триггерами СУБД.
/// Полностью автономен, потокобезопасен и скрыт от прикладного программиста.
/// </summary>
public class UiDataBroker<TEntity, TQueryState, TResultData> : IUiDataBroker<TEntity, TQueryState, TResultData>, IDisposable
    where TEntity : class, new()
{
    // --- ДЕЛЕГАТЫ ЯДРА (СИНХРОНИЗИРОВАНЫ С DATA-CONTEXT) ---
    private Func<TQueryState, CancellationToken, Task<TResultData>>? _serverDataProvider;
    private Func<CancellationToken, Task<List<TEntity>>>? _serverDataLoader;
    private Func<IReadOnlyList<TEntity>, TQueryState, TResultData>? _inMemoryEvaluator;

    private IUiOzuCache<TEntity>? _ozuCache;
    private readonly Action? _onDataChangedNotifier;

    private bool _isInitialized;
    private bool _isOzuLoaded;

    // Системный семафор для предотвращения Race Condition при стартовом прогреве кэша
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    // Инфраструктура дебаунса сетевых уведомлений для ServerMode
    private CancellationTokenSource? _debounceCts;
    private readonly object _debounceLock = new();
    private const int DebounceDelayMs = 300;

    /// <summary>
    /// Режим работы определяется нативно: если провайдер сервера пуст, значит мы в памяти.
    /// </summary>
    public bool IsInMemoryMode => _serverDataProvider == null && _isInitialized;

    public UiDataBroker(Action? onDataChangedNotifier = null)
    {
        _onDataChangedNotifier = onDataChangedNotifier;
        // Мягкая регистрация на глобальные триггеры СУБД
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
        _isInitialized = true;
        _isOzuLoaded = false;
    }

    public void ConfigureInMemoryMode(
        IUiOzuCache<TEntity> ozuCache,
        Func<CancellationToken, Task<List<TEntity>>> serverDataLoader,
        Func<IReadOnlyList<TEntity>, TQueryState, TResultData> inMemoryEvaluator)
    {
        _ozuCache = ozuCache ?? throw new ArgumentNullException(nameof(ozuCache));
        _serverDataLoader = serverDataLoader ?? throw new ArgumentNullException(nameof(serverDataLoader));
        _inMemoryEvaluator = inMemoryEvaluator ?? throw new ArgumentNullException(nameof(inMemoryEvaluator));
        _serverDataProvider = null;
        _isInitialized = true;
        _isOzuLoaded = false;
    }

    public async Task<TResultData> FetchDataAsync(TQueryState state, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException($"Брокер данных для '{typeof(TEntity).Name}' не был сконфигурирован.");
        }

        // --- РЕЖИМ А: ПРЯМОЙ СЕРВЕРНЫЙ ТРАНСПОРТ (SERVER-SIDE) ---
        if (_serverDataProvider != null)
        {
            return await _serverDataProvider(state, cancellationToken);
        }

        // --- РЕЖИМ Б: ОПЕРАТИВНЫЙ КЭШ В ОЗУ (IN-MEMORY) ---
        if (_ozuCache != null && _inMemoryEvaluator != null && _serverDataLoader != null)
        {
            if (!_isOzuLoaded)
            {
                // Блокируем параллельные потоки Blazor Server на время ленивого gRPC-запроса
                await _cacheLock.WaitAsync(cancellationToken);
                try
                {
                    if (!_isOzuLoaded)
                    {
                        List<TEntity> initialData = await _serverDataLoader(cancellationToken);
                        _ozuCache.Initialize(initialData);
                        _isOzuLoaded = true;
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }

            // Безопасно вычисляем LINQ-стейт внутри внутреннего локального замка OzuCache
            return _ozuCache.ExecuteInLock(items => _inMemoryEvaluator(items, state));
        }

        throw new InvalidOperationException("Критический сбой внутренней конфигурации брокера данных.");
    }

    public void HandleDatabaseCommit(EntityStateChangeEnum state, object entity)
    {
        if (entity is not TEntity typedEntity) return;

        // Физика: Отсутствие серверного провайдера гарантирует In-Memory режим
        if (_serverDataProvider == null)
        {
            // Мгновенно обновляем ОЗУ дельтой из базы данных для сохранения консистентности
            if (_isOzuLoaded && _ozuCache != null)
            {
                _ozuCache.ApplyOzuDelta(state, typedEntity);
            }

            // Мгновенно пинаем UI Blazor Server
            _onDataChangedNotifier?.Invoke();
        }
        else
        {
            // Режим прямой работы с сервером: защищаем gRPC-каналы дебаунсом в 300 мс
            lock (_debounceLock)
            {
                if (!_isInitialized) return;

                _debounceCts?.Cancel();
                _debounceCts?.Dispose();

                _debounceCts = new CancellationTokenSource();
                CancellationToken token = _debounceCts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(DebounceDelayMs, token);

                        if (!token.IsCancellationRequested)
                        {
                            _onDataChangedNotifier?.Invoke();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Мягко глотаем отмену таски при пачке изменений в БД
                    }
                }, CancellationToken.None);
            }
        }
    }

    public void Dispose()
    {
        // Отписываемся от статического триггера ядра, предотвращая утечку всего контекста экрана
        DatabaseTriggerService.OnEntityCommitted -= HandleGlobalEntityCommitted;

        lock (_debounceLock)
        {
            _isInitialized = false;

            if (_debounceCts != null)
            {
                _debounceCts.Cancel();
                _debounceCts.Dispose();
                _debounceCts = null;
            }
        }

        // Хирургически зачищаем системный семафор прогрева кэша
        _cacheLock.Dispose();
    }
}