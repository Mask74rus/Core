using MudBlazor;
using Promatis.Net.Data;
using System.Reflection;

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

    private bool _isInitialized;
    private bool _isInMemoryMode;

    public bool IsInMemoryMode => _isInMemoryMode;

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
        // КРИТИЧЕСКОЕ ТРЕБОВАНИЕ: Если программист забыл вызвать метод конфигурации — жестко падаем!
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                $"Брокер данных для '{typeof(TEntity).Name}' не был сконфигурирован. " +
                $"Вы обязаны вызвать ConfigureServerMode или ConfigureInMemoryMode в конструкторе контекста формы.");
        }

        // РЕЖИМ 1: Прямая работа с сервером (База данных / API)
        if (_serverDataProvider != null)
        {
            return await _serverDataProvider(state, cancellationToken);
        }

        // РЕЖИМ 2: Работа через локальный ОЗУ-кэш формы (Стратегия расчета полностью снаружи!)
        if (_isInMemoryMode && _ozuCache != null && _inMemoryEvaluator != null)
        {
            // Просто отдаем стейт и ОЗУ-список наружу — пусть прикладной селектор сам решает,
            // как сделать пагинацию грида или вытащить детей дерева
            return _inMemoryEvaluator(state, _ozuCache.InMemoryItems);
        }

        throw new InvalidOperationException("Критический сбой внутренней конфигурации брокера данных.");
    }

    public void HandleDatabaseCommit(EntityStateChangeEnum state, object entity)
    {
        // Если форма работает напрямую с сервером — глобальные мутации ОЗУ полностью игнорируются!
        if (!_isInMemoryMode || _ozuCache == null) return;

        // Проверяем: относится ли прилетевший из СУБД объект к типу данных текущего экрана?
        if (entity is TEntity typedEntity)
        {
            // Перенаправляем дельту изменений в изолированное хранилище этой конкретной вкладки
            _ozuCache.ApplyOzuDelta(state, typedEntity);
        }
    }
}