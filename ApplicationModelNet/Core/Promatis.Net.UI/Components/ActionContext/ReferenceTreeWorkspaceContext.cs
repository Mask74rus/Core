using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.UI.Controls;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Бизнес-Агрегатор для всех древовидных (иерархических) справочников НСИ системы.
/// Фиксирует типы данных, настраивает рекурсивный ОЗУ-кэш и автоматически собирает дерево-тулбар.
/// </summary>
public abstract class ReferenceTreeWorkspaceContext<TEntity> : TreeWorkspaceContext<TEntity, Guid>
    where TEntity : ReferenceTreeBase<TEntity>, new()
{
    protected ReferenceTreeWorkspaceContext(IServiceProvider serviceProvider, Action? onDataChangedNotifier = null)
        : base(
            serviceProvider,
            isInMemoryMode: true,
            // Передаем строго типизированную стратегию обхода графа дерева для ОЗУ-кэша ядра
            treeStrategy: new HierarchicalOzuMutationStrategy<TEntity>(
                parentIdSelector: x => x.ParentId,
                childrenSelector: x => x.Children.ToList()
            ),
            onDataChangedNotifier: onDataChangedNotifier)
    {
        // Автоматически наполняем тулбар специализированными командами управления деревом
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

        // 3. Кнопка изменения выделенного узла
        AddControl(new EditEntityButton<TEntity>().OnExecute(async (row) => await ExecuteEditRecordAsync(row)));

        // 4. Кнопка удаления ( MessageBox подтверждения и команда удаления в СУБД)
        AddControl(new DeleteEntityButton<TEntity>().OnExecute(async (row) => await ExecuteDeleteRecordAsync(row)));

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

        // Создаем чистый объект, но жестко привязываем его к текущему выделенному узлу
        var childModel = new TEntity
        {
            ParentId = SelectedData.Id
        };

        await OpenDialogWindowAsync(childModel, isNew: true);
    }
}