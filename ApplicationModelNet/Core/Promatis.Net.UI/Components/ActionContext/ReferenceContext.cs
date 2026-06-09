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
public abstract class ReferenceContext<TEntity> : GridContext<TEntity, Guid>, IToolbarContext
    where TEntity : ReferenceBase, new()
{
    private readonly IReferenceService<TEntity> _referenceService;

    // --- ОБЕСПЕЧЕНИЕ ИНТЕРФЕЙСА IToolbarContext ФИЗИЧЕСКОЙ ПАМЯТЬЮ СИЛАМИ ЯДРА ---
    public Lock ControlsLock { get; } = new();
    public List<IUiControl> InnerControls { get; } = [];
    public bool IsToolbarInitialized { get; set; }

    protected ReferenceContext(IServiceProvider serviceProvider, bool isInMemoryMode = true)
        : base(serviceProvider, isInMemoryMode)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        // Возвращаем ваш реальный доменный сервис из DI-сессии
        _referenceService = serviceProvider.GetRequiredService<IReferenceService<TEntity>>();

        var strategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(strategy);
    }

    /// <summary>
    /// ЧИСТЫЙ МЕТОД СБОРКИ ТУЛБАРА.
    /// Вызывается интерфейсом лениво в полной рантайм-тишине без блокировок и рекурсий.
    /// </summary>
    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        controls.Add(new CreateEntityButton<TEntity>());
        controls.Add(new EditEntityButton<TEntity>());
        controls.Add(new DeleteEntityButton<TEntity>());

        // Безопасный хук расширения для конкретных прикладных справочников
        AddInitializeContext();
    }

    protected virtual void AddInitializeContext() { }

    // --- ОБЯЗАТЕЛЬНЫЕ ФУНКЦИОНАЛЬНЫЕ МОСТЫ ТРАНСПОРТА (ДЛЯ БРОКЕРА) ---

    protected override async Task<GridData<TEntity>> FetchDataFromServerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        // Честный серверный транспорт выгрузки страниц (для редких тяжелых справочников без ОЗУ-кэша)
        var pagedResult = await _referenceService.GetPagedAsync(state.Page, state.PageSize, ct);
        return new GridData<TEntity> { Items = pagedResult.Items, TotalItems = pagedResult.TotalCount };
    }

    protected override async Task<List<TEntity>> LoadInitialDataForBrokerAsync(GridState<TEntity> state, CancellationToken ct)
    {
        // ИСПРАВЛЕНО: Ленивый поставщик сырых данных на вашем реальном сервисе
        try { return await _referenceService.GetAllAsync(ct) ?? []; }
        catch (OperationCanceledException) { return []; }
    }
}