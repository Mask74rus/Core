using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.UI.Controls;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех плоских справочников НСИ системы.
/// Фиксирует типы данных, включает InMemory Mode и автоматически собирает типовой CRUD-тулбар.
/// </summary>
public abstract class ReferenceWorkspaceContext<TEntity> : GridWorkspaceContext<TEntity, Guid>
    where TEntity : ReferenceBase, new()
{
    protected ReferenceWorkspaceContext(IServiceProvider serviceProvider, Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode: true, onDataChangedNotifier: onDataChangedNotifier)
    {
        // Принудительно наполняем тулбар бизнес-кнопками по умолчанию для справочника
        PopulateDefaultCrudToolbar();
    }

    /// <summary>
    /// Автоматическая сборка панели управления.
    /// Конечные справочники (Шаг 5) при желании могут очистить коллекцию или добавить свои уникальные кнопки.
    /// </summary>
    protected virtual void PopulateDefaultCrudToolbar()
    {
        // Кнопка Создать — вызывает готовую проводку ядра
        AddControl(new CreateEntityButton<TEntity>().OnExecute(ExecuteCreateRecordAsync));

        // Кнопка Изменить — автоматически завязывается на SelectedData в GridPage
        AddControl(new EditEntityButton<TEntity>().OnExecute(async (row) => await ExecuteEditRecordAsync(row)));

        // Кнопка Удалить — вызывает MessageBox и команду удаления в СУБД
        AddControl(new DeleteEntityButton<TEntity>().OnExecute(async (row) => await ExecuteDeleteRecordAsync(row)));

        // Стандартный визуальный разделитель
        AddControl(new ToolbarDivider());
    }
}