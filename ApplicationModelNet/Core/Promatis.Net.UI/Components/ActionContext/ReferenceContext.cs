using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех плоских справочников НСИ системы.
/// Фиксирует типы данных, включает InMemory Mode и автоматически собирает типовой CRUD-тулбар.
/// </summary>
public abstract class ReferenceContext<TEntity> : GridContext<TEntity, Guid>
    where TEntity : ReferenceBase, new()
{
    private readonly IReferenceService<TEntity> _referenceService;
    private readonly Lock _controlsLock = new();
    private readonly List<IUiControl> _controls = [];
    private bool _isToolbarInitialized;

    /// <summary>
    /// Переопределение свойства Controls. Возвращает строго упорядоченную коллекцию 
    /// интерактивных CRUD-кнопок плоского справочника НСИ.
    /// </summary>
    public IEnumerable<IUiControl> Controls
    {
        get
        {
            lock (_controlsLock)
            {
                if (!_isToolbarInitialized)
                {
                    InitializeContext(); // Лениво собираем тулбар кнопок при первом рендере панели
                    _isToolbarInitialized = true;
                }
                return _controls.ToArray(); // Изолированный снапшот
            }
        }
    }

    protected ReferenceContext(IServiceProvider serviceProvider, bool isInMemoryMode = true)
        : base(serviceProvider, isInMemoryMode) // По умолчанию справочники НСИ всегда кэшируются в ОЗУ
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Внедряем базовый CRUD-сервис репозитория СУБД из DI сессии
        _referenceService = serviceProvider.GetRequiredService<IReferenceService<TEntity>>();

        // Извлекаем и жестко связываем ОЗУ-стратегию мутаций плоских справочников
        var strategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(strategy);
    }

    /// <summary>
    /// ВНУТРЕННЯЯ СБОРКА ТУЛБАРА НСИ.
    /// Наполняет шину управления стандартными умными CRUD-кнопками ядра 
    /// и безопасно передает управление прикладным наследникам внутри защищенного lock-периметра.
    /// </summary>
    private void InitializeContext()
    {
        lock (_controlsLock)
        {
            _controls.Add(new CreateEntityButton<TEntity>());
            _controls.Add(new EditEntityButton<TEntity>());
            _controls.Add(new DeleteEntityButton<TEntity>());

            // Нативно вызываем безопасную точку расширения для конкретных справочников
            AddInitializeContext();
        }
    }

    /// <summary>
    /// ПРИКЛАДНАЯ ТОЧКА РАСШИРЕНИЯ ТУЛБАРА.
    /// Конкретный справочник переопределяет этот метод, чтобы безопасно добавить на панель 
    /// свои специфичные фильтры, селекторы или кнопки действий (они встанут правее CRUD-блока).
    /// </summary>
    protected virtual void AddInitializeContext()
    {
        // По умолчанию пустой справочник не добавляет кастомных элементов
    }

    // --- МОНОПОЛЬНЫЙ АВТОМАТ CRUD-КОНВЕЙЕРА ВСЕХ СПРАВОЧНИКОВ ПЛАТФОРМЫ ---

    /// <summary>
    /// Финальный сквозной конвейер фиксации изменений проверенного черновика в PostgreSQL.
    /// Вызывается обобщенным ядром страницы (ReferencePageBase) после успешного прохождения FluentValidation.
    /// </summary>
    public async Task CommitActionAsync()
    {
        if (DraftData == null) return;

        // Извлекаем Guid-ключ черновика домена через хелпер базового ядра
        Guid currentKey = GetEntityKey(DraftData);

        // Если ключ пустой (Guid.Empty) — это нативный признак СОЗДАНИЯ новой записи
        bool isNewEntity = currentKey == Guid.Empty;

        if (isNewEntity)
        {
            // Вызываем вставку в PostgreSQL через gRPC/API сервис инфраструктуры
            await _referenceService.AddAsync(DraftData);
        }
        else
        {
            // Вызываем изменение в PostgreSQL через gRPC/API сервис инфраструктуры
            await _referenceService.UpdateAsync(DraftData);
        }

        // Полностью очищаем буфер мутации в памяти
        DraftData = default;

        // Бьем в единый колокол — Брокер применит дельту к ОЗУ, и Blazor нативно обновит экран!
        NotifyContextUpdated();
    }

    /// <summary>
    /// Универсальный конвейер асинхронного удаления записи из PostgreSQL для справочников НСИ.
    /// Вызывается пассивной кнопкой-командой тулбара (DeleteEntityButton).
    /// </summary>
    public virtual async Task ExecuteDeleteActionAsync(TEntity? targetEntity)
    {
        if (targetEntity == null) return;

        Guid targetKey = GetEntityKey(targetEntity);

        // Физически или мягко стираем запись из PostgreSQL через сервис инфраструктуры
        await _referenceService.DeleteAsync(targetKey);

        // Синхронизируем ОЗУ-кэш и пинаем UI-поток
        NotifyContextUpdated();
    }

    // --- РЕАЛИЗАЦИЯ АБСТРАКТНЫХ СЕТЕВЫХ ТРАНСПОРТНЫХ МОСТОВ К СУБД ---

    /// <summary>
    /// Честный серверный транспорт. Вызывается Брокером, если справочник переведен в Server Mode.
    /// </summary>
    protected override async Task<GridData<TEntity>> FetchDataFromServerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        var pagedResult = await _referenceService.GetPagedAsync(state.Page, state.PageSize, ct);

        return new GridData<TEntity>
        {
            Items = pagedResult.Items,
            TotalItems = pagedResult.TotalCount
        };
    }

    /// <summary>
    /// Ленивый поставщик сырых данных для первоначального наполнения Брокером ОЗУ-кэша справочника.
    /// </summary>
    protected override async Task<List<TEntity>> LoadInitialDataForBrokerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        try
        {
            return await _referenceService.GetAllAsync(ct) ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    // --- УПРАВЛЕНИЕ КОЛЛЕКЦИЕЙ КОНТРОЛОВ ДЛЯ ПОТОМКОВ ---

    protected void AddControl(IUiControl control)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));
        lock (_controlsLock) _controls.Add(control);
        NotifyContextUpdated();
    }

    protected void RemoveControl(string controlId)
    {
        if (string.IsNullOrWhiteSpace(controlId)) return;
        lock (_controlsLock) _controls.RemoveAll(c => c.Id == controlId);
        NotifyContextUpdated();
    }
}