using Promatis.Net.Domain;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех плоских справочников НСИ системы.
/// Фиксирует типы данных, включает InMemory Mode и автоматически собирает типовой CRUD-тулбар.
/// </summary>
public abstract class ReferenceContext<TEntity> : GridContext<TEntity, Guid>
    where TEntity : ReferenceBase, new()
{
    // ИСПРАВЛЕНО: Параметр isInMemoryMode теперь гибко управляется через конструктор наследников, 
    // позволяя справочникам НСИ использовать все преимущества кэширования в ОЗУ.
    protected ReferenceContext(
        IServiceProvider serviceProvider,
        bool isInMemoryMode,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode, onDataChangedNotifier)
    {
    }

    /// <summary>
    /// Автоматически вызывается базовым холстом (WorkspacePage) в событии OnInitialized,
    /// когда граф объектов наследников уже гарантированно построен в памяти.
    /// </summary>
    public override void InitializeContext()
    {
        PopulateDefaultCrudToolbar();
    }

    /// <summary>
    /// Автоматическая сборка панели управления.
    /// Конечные справочники при желании могут переопределить метод, очистить коллекцию или добавить свои уникальные кнопки.
    /// </summary>
    protected virtual void PopulateDefaultCrudToolbar()
    {
        // Кнопка Создать — вызывает готовую проводку ядра
        AddControl(new CreateEntityButton<TEntity>().OnExecute(ExecuteCreateRecordAsync));

        // ИСПРАВЛЕНО: Полностью избавились от анонимных лямбда-замыканий async (row) => ...
        // Ссылки на базовые CRUD-команды передаются напрямую, страхуя GC от утечек памяти.
        AddControl(new EditEntityButton<TEntity>().OnExecute(ExecuteEditRecordAsync));

        // Кнопка Удалить — запрашивает MessageBox и выполняет команду удаления
        AddControl(new DeleteEntityButton<TEntity>().OnExecute(ExecuteDeleteRecordAsync));

        // Стандартный визуальный разделитель
        AddControl(new ToolbarDivider());
    }
}