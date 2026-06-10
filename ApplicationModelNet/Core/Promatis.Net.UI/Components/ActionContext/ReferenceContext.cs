using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.Service;
using Promatis.Net.UI.Controls;
using static MudBlazor.CategoryTypes;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех плоских справочников НСИ системы.
/// Фиксирует типы данных, включает InMemory Mode и автоматически собирает типовой CRUD-тулбар.
/// </summary>
public abstract class ReferenceContext<TEntity> : GridContext<TEntity>, IToolbarContext
    where TEntity : ReferenceBase, new()
{
    private readonly IReferenceService<TEntity> _referenceService;
    protected readonly IDialogService DialogService;
    protected readonly IServiceProvider ServiceProvider; // Сохраняем IoC-сессию для лямбда-команд кнопок

    // ====================================================================================
    // --- РЕАЛИЗАЦИЯ ФИЗИЧЕСКОЙ ПАМЯТИ ДЛЯ КОНТРАКТА IToolbarContext ---
    // ====================================================================================

    /// <summary>
    /// Потокобезопасный замок синхронизации. Считывается дефолтной логикой интерфейса.
    /// </summary>
    public System.Threading.Lock ControlsLock { get; } = new();

    /// <summary>
    /// Мутабельный внутренний список кнопок ядра.
    /// </summary>
    public List<IUiControl> InnerControls { get; } = [];

    /// <summary>
    /// Флаг, сигнализирующий о том, что стартовый пакет кнопок уже собран.
    /// </summary>
    public bool IsToolbarInitialized { get; set; }

    /// <summary>
    /// Конструктор абстрактного ядра табличных справочников.
    /// </summary>
    protected ReferenceContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _referenceService = serviceProvider.GetRequiredService<IReferenceService<TEntity>>();
        DialogService = serviceProvider.GetRequiredService<IDialogService>();

        // Связываем специализированную стратегию мутаций ОЗУ (нужна для ApplyOzuDelta в Брокере данных)
        var strategy = serviceProvider.GetRequiredService<IOzuMutationStrategy<TEntity>>();
        OzuCache.SetMutationStrategy(strategy);
    }

    // ====================================================================================
    // --- МЕТОД СБОРКИ СЛЕПЫХ КНОПОК ТУЛБАРА ---
    // ====================================================================================

    /// <summary>
    /// Чистый метод сборки тулбара. Вызывается интерфейсом лениво при первом рендере UI.
    /// Наполняет мутабельный список без риска гонок потоков.
    /// </summary>
    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        // 1. КНОПКА "СОЗДАТЬ": Инициализирует чистый объект в теле бизнес-команды
        controls.Add(new CreateEntityButton<TEntity>().OnExecute(async () =>
        {
            var newEntity = new TEntity();
            await OpenDialogFormAsync(newEntity);
        }));

        // 2. КНОПКА "РЕДАКТИРОВАТЬ": Извлекает системный клонер платформы из IoC и изолирует стейт
        controls.Add(new EditEntityButton<TEntity>().OnExecute(async (typedEntity) =>
        {
            // Извлекаем клонер платформы из сохраненного ServiceProvider ядра данных
            var entityCloner = ServiceProvider.GetRequiredService<IEntityCloner>();

            // Рождение объекта диалога (изолированного клона) происходит строго внутри бизнес-команды!
            var clone = entityCloner.CloneEntity(typedEntity);
            await OpenDialogFormAsync(clone);
        }));

        // 3. КНОПКА "УДАЛИТЬ": Выполняет прямое gRPC-удаление сущности
        controls.Add(new DeleteEntityButton<TEntity>().OnExecute(async (typedEntity) =>
        {
            await _referenceService.DeleteAsync(typedEntity.Id);
            SelectedData = null; // Мгновенно сбрасываем селекшен строки таблицы в UI после физического удаления
        }));

        // Мягкий защищенный хук расширения для уникальных кнопок конкретных прикладных справочников
        AddInitializeContext(controls);
    }

    /// <summary>
    /// Абстрактный метод вызова диалогового окна. Каждое конкретное поддерево справочников 
    /// переопределит его для вызова своего уникального визуального файла MudDialog (например, UserDialog).
    /// </summary>
    protected abstract Task OpenDialogFormAsync(TEntity model);

    /// <summary>
    /// Виртуальный хук расширения тулбара для добавления кастомных бизнес-кнопок.
    /// </summary>
    protected virtual void AddInitializeContext(List<IUiControl> controls) { }
}