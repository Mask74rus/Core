using Promatis.Net.Domain;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех древовидных (иерархических) справочников НСИ системы.
/// Фиксирует типы данных, настраивает рекурсивный ОЗУ-кэш и автоматически собирает дерево-тулбар.
/// </summary>
public abstract class ReferenceTreeContext<TEntity> : TreeContext<TEntity, Guid>
    where TEntity : ReferenceTreeBase<TEntity>, new()
{
    // ИСПРАВЛЕНО: Конструктор стал кристально чистым. Настройка стратегий мутаций 
    // и вызовы виртуальных методов отсюда полностью удалены!
    protected ReferenceTreeContext(
        IServiceProvider serviceProvider,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode: true, onDataChangedNotifier)
    {
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Безопасный метод жизненного цикла контекста.
    /// Автоматически вызывается базовым холстом (WorkspacePage) в событии OnInitialized,
    /// гарантируя защиту классов-наследников от NullReferenceException.
    /// </summary>
    public override void InitializeContext()
    {
        PopulateDefaultTreeToolbar();
    }

    /// <summary>
    /// Автоматическая сборка панели управления для дерева.
    /// </summary>
    protected virtual void PopulateDefaultTreeToolbar()
    {
        // 1. Кнопка создания корневого элемента (без родителя)
        AddControl(new CreateEntityButton<TEntity> { Title = "Добавить корень" }
            .OnExecute(ExecuteCreateRecordAsync));

        // 2. Кнопка создания дочернего подузла (автоматически завязана на наличие SelectedData)
        AddControl(new CreateChildButton<TEntity>().OnExecute(ExecuteCreateChildRecordAsync));

        // 3. ИСПРАВЛЕНО: Прямая передача ссылок на методы без анонимных лямбда-замыканий
        AddControl(new EditEntityButton<TEntity>().OnExecute(ExecuteEditRecordAsync));

        // 4. Кнопка удаления (MessageBox подтверждения и команда удаления в СУБД)
        AddControl(new DeleteEntityButton<TEntity>().OnExecute(ExecuteDeleteRecordAsync));

        // Стандартный визуальный разделитель
        AddControl(new ToolbarDivider());
    }

    /// <summary>
    /// Специфичная бизнес-команда создания подчиненного узла.
    /// Считывает ID выделенного родителя и записывает его в ParentId новой сущности перед вызовом окна.
    /// </summary>
    protected async Task ExecuteCreateChildRecordAsync()
    {
        if (SelectedData == null) return;

        // Создаем чистый объект, используя новый синтаксис C# целевого типа, 
        // и привязываем его к текущему выделенному узлу
        TEntity childModel = new()
        {
            ParentId = SelectedData.Id
        };

        await OpenDialogWindowAsync(childModel, isNew: true);
    }
}