using Promatis.Net.Domain;
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
    // --- ОБЕСПЕЧЕНИЕ ИНТЕРФЕЙСА IToolbarContext ФИЗИЧЕСКОЙ ПАМЯТЬЮ ---
    public Lock ControlsLock { get; } = new();
    public List<IUiControl> InnerControls { get; } = [];
    public bool IsToolbarInitialized { get; set; }

    protected ReferenceTreeContext(IServiceProvider serviceProvider)
        : base(serviceProvider, isInMemoryMode: true)
    {
    }

    /// <summary>
    /// ЧИСТЫЙ МЕТОД СБОРКИ ТУЛБАРА ДЕРЕВА.
    /// Сигнатуры групп методов разделены строго по типам ожидания кнопок ядра Promatis.
    /// </summary>
    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        // 1. Кнопки создания ожидают чистый Func<Task>. Сигнатура: () => Task
        controls.Add(new CreateEntityButton<TEntity> { Title = "Добавить корень" }
            .OnExecute(ExecuteCreateRecordAsync));

        controls.Add(new CreateChildButton<TEntity>()
            .OnExecute(ExecuteCreateChildRecordAsync));

        // 2. Кнопки мутаций ожидают Func<TEntity?, Task>. Сигнатура: (entity) => Task
        controls.Add(new EditEntityButton<TEntity>()
            .OnExecute(ExecuteEditRecordAsync));

        controls.Add(new DeleteEntityButton<TEntity>()
            .OnExecute(ExecuteDeleteRecordAsync));

        controls.Add(new ToolbarDivider());
        AddInitializeContext();
    }

    protected virtual void AddInitializeContext() { }

    // --- БИЗНЕС-КОМАНДЫ ИНИЦИАЛИЗАЦИИ ЧЕРНОВИКОВ (БЕЗ ПАРАМЕТРОВ, ДЛЯ СОЗДАНИЯ) ---

    /// <summary>
    /// Команда создания КОРНЕВОГО элемента дерева. Соответствует сигнатуре Func<Task>.
    /// </summary>
    protected virtual Task ExecuteCreateRecordAsync()
    {
        DraftData = new TEntity();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Команда создания ПОДЧИНЕННОГО узла графа. Соответствует сигнатуре Func<Task>.
    /// </summary>
    protected virtual Task ExecuteCreateChildRecordAsync()
    {
        if (SelectedData == null) return Task.CompletedTask;

        DraftData = new TEntity
        {
            ParentId = SelectedData.Id
        };
        return Task.CompletedTask;
    }

    // --- БИЗНЕС-КОМАНДЫ МУТАЦИЙ ЖИВЫХ СТРОК (С ПАРАМЕТРОМ TEntity?) ---

    /// <summary>
    /// Команда редактирования узла. Соответствует сигнатуре Func<TEntity?, Task>.
    /// </summary>
    protected virtual Task ExecuteEditRecordAsync(TEntity? entity)
    {
        var target = entity ?? SelectedData;
        if (target == null) return Task.CompletedTask;

        DraftData = target;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Команда удаления узла. Соответствует сигнатуре Func<TEntity?, Task>.
    /// </summary>
    protected virtual Task ExecuteDeleteRecordAsync(TEntity? entity)
    {
        var target = entity ?? SelectedData;
        if (target == null) return Task.CompletedTask;

        return Task.CompletedTask;
    }
}