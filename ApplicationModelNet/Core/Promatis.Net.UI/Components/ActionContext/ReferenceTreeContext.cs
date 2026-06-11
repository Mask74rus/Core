using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain.Interface;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех древовидных (иерархических) справочников НСИ системы.
/// Фиксирует типы данных, настраивает рекурсивный ОЗУ-кэш и автоматически собирает дерево-тулбар.
/// </summary>
public abstract class ReferenceTreeContext<TEntity> : TreeContext<TEntity>, IToolbarContext
    where TEntity : class, ITreeNode<TEntity>, new()
{
    protected readonly IDialogService DialogService;
    protected readonly IServiceProvider ServiceProvider; // Сохраняем IoC-сессию для извлечения клонера внутри лямбд

    // ====================================================================================
    // --- РЕАЛИЗАЦИЯ ФИЗИЧЕСКОЙ ПАМЯТИ ДЛЯ КОНТРАКТА IToolbarContext ---
    // ====================================================================================

    public System.Threading.Lock ControlsLock { get; } = new();
    public List<IUiControl> InnerControls { get; } = [];
    public bool IsToolbarInitialized { get; set; }

    /// <summary>
    /// Конструктор абстрактного ядра древовидных справочников.
    /// </summary>
    protected ReferenceTreeContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        DialogService = serviceProvider.GetRequiredService<IDialogService>();

        // ВНИМАНИЕ: Настройка InMemory/ServerSide стратегий получения данных 
        // делегирована исключительно конечному прикладному классу!
    }

    // ====================================================================================
    // --- МЕТОД СБОРКИ СЛЕПЫХ КНОПОК ТУЛБАРА ДЕРЕВА ---
    // ====================================================================================

    /// <summary>
    /// Чистый метод сборки тулбара дерева. Вызывается интерфейсом лениво при первом рендере UI.
    /// Рождение объектов для диалогов (New/Clone/Child) инкапсулировано внутри бизнес-команд.
    /// </summary>
    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        // 1. КНОПКА "ДОБАВИТЬ КОРЕНЬ": Создает пустую сущность, у которой ParentId гарантированно null
        controls.Add(new CreateEntityButton<TEntity> { Title = "Добавить корень" }.OnExecute(async () =>
        {
            var newRoot = new TEntity();
            await OpenDialogFormAsync(newRoot);
        }));

        // 2. КНОПКА "ДОБАВИТЬ ПОДЧИНЕННЫЙ УЗЕЛ": АКТИВНА при наличии селекшена. Привязывает ParentId к выбранной строке.
        controls.Add(new CreateChildButton<TEntity>().OnExecute(async () =>
        {
            if (SelectedData == null) return;

            var newChild = new TEntity
            {
                ParentId = SelectedData.Id // Жесткая иерархическая ООП-привязка к родителю
            };
            await OpenDialogFormAsync(newChild);
        }));

        // 3. КНОПКА "РЕДАКТИРОВАТЬ": Извлекает системный клонер платформы из IoC и изолирует стейт узла
        controls.Add(new EditEntityButton<TEntity>().OnExecute(async (typedEntity) =>
        {
            if (SelectedData == null) return;

            var entityCloner = ServiceProvider.GetRequiredService<IEntityCloner>();

            // Рождение объекта диалога (изолированного клона) происходит строго внутри бизнес-команды контекста!
            var clone = entityCloner.CloneEntity(SelectedData);
            await OpenDialogFormAsync(clone);
        }));

        // 4. КНОПКА "УДАЛИТЬ": Точка расширения для удаления (может вызывать доменный gRPC сервис)
        controls.Add(new DeleteEntityButton<TEntity>().OnExecute(async (typedEntity) =>
        {
            if (SelectedData == null) return;
            await ExecuteDeleteNodeInternalAsync(SelectedData);
            SelectedData = null; // Сбрасываем селекшен после удаления узла графа
        }));

        controls.Add(new ToolbarDivider());

        // Мягкий защищенный хук расширения для уникальных кнопок конкретных прикладных деревьев
        AddInitializeContext(controls);
    }

    /// <summary>
    /// Абстрактный метод вызова диалогового окна. Каждое конкретное дерево 
    /// переопределит его для вызова своего уникального визуального файла MudDialog.
    /// </summary>
    protected abstract Task OpenDialogFormAsync(TEntity model);

    /// <summary>
    /// Виртуальный метод удаления узла. Переопределяется на конечном слое для gRPC вызова.
    /// </summary>
    protected virtual Task ExecuteDeleteNodeInternalAsync(TEntity entity) => Task.CompletedTask;

    /// <summary>
    /// Виртуальный хук расширения тулбара для добавления кастомных древовидных бизнес-кнопок.
    /// </summary>
    protected virtual void AddInitializeContext(List<IUiControl> controls) { }
}