using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;
using Promatis.Net.Test.MDM.Domain;
using Promatis.Net.UI.Components.BaseToolbarWorkspacePage;
using Promatis.Net.UI.Components.BaseTree;

namespace Promatis.Net.Test.MDM.UI.Unit;

public partial class UnitTreePage : ComponentBase
{
    [Inject] private IUnitBaseService<MdmApplicationDbContext> UnitService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    protected UnitTreeActionContext Context { get; } = new()
    {
        PageTitle = "Структура предприятия и оборудования",
        Position = ToolbarPosition.Top
    };

    private InMemoryTreeEngine<UnitBase> _treeEngine = null!;
    private bool _isInitialLoading = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _treeEngine = new InMemoryTreeEngine<UnitBase>(
            idSelector: x => x.Id,
            parentIdSelector: x => x.ParentId,
            syncNavigation: (parent, child) => { parent.Children.Add(child); child.Parent = parent; },
            clearChildren: x => x.Children.Clear(),
            removeChildAction: (parent, child) => parent.Children.Remove(child) // <-- ДОБАВЛЕН ПЯТЫЙ АРГУМЕНТ
        );
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadInitialDataAsync();
        }
    }

    private async Task LoadInitialDataAsync()
    {
        _isInitialLoading = true;
        StateHasChanged();
        try
        {
            List<UnitBase> allItems = await UnitService.GetAllAsync();
            _treeEngine.Initialize(allItems);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка загрузки структуры: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isInitialLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Чистый, легкий перехватчик инкрементальных обновлений СУБД.
    /// Передает задачу универсальному ОЗУ-движку.
    /// </summary>
    private Task HandleIncrementalUpdateAsync((EntityStateChangeEnum State, UnitBase Entity) delta)
    {
        _treeEngine.ApplyDelta(delta.State, delta.Entity);

        // Пересчитываем стейт кнопок тулбара на основе измененных в ОЗУ данных
        Context.SelectedData = Context.SelectedData;

        StateHasChanged();
        return Task.CompletedTask;
    }

    // =========================================================================
    // CRUD ОПЕРАЦИИ И ДИАЛОГИ
    // =========================================================================

    protected async Task CreateRootNodeAsync()
    {
        var newRoot = new DepartmentUnit { ParentId = null, Type = UnitType.Workshop };
        await ShowDialogAsync("Новый корневой элемент", newRoot, true, UnitService.AddAsync);
    }

    protected async Task CreateChildNodeAsync()
    {
        if (Context.SelectedData == null) return;
        UnitBase selected = Context.SelectedData;

        UnitKind childKind = selected.Kind switch
        {
            UnitKind.Department => UnitKind.Production,
            UnitKind.Production => UnitKind.Position,
            UnitKind.Storage => UnitKind.Storage,
            UnitKind.Transport => UnitKind.Position,
            _ => UnitKind.Position
        };

        UnitBase childUnit = childKind switch
        {
            UnitKind.Department => new DepartmentUnit { Type = UnitType.Other, ParentId = selected.Id },
            UnitKind.Production => new ProductionUnit { Type = UnitType.Other, ParentId = selected.Id },
            UnitKind.Storage => new StorageUnit { Type = UnitType.Other, ParentId = selected.Id },
            UnitKind.Transport => new TransportUnit { Type = UnitType.Other, ParentId = selected.Id },
            UnitKind.Position => new PositionUnit { Type = UnitType.Other, ParentId = selected.Id },
            _ => throw new ArgumentOutOfRangeException(nameof(childKind), $"Неизвестная категория {childKind}")
        };

        childUnit.Parent = null;
        await ShowDialogAsync($"Добавление подузла для {selected.Name}", childUnit, true, UnitService.AddAsync);
        Context.SelectedData = null;
    }

    protected async Task EditSelectedNodeAsync()
    {
        if (Context.SelectedData != null) await EditNodeAsync(Context.SelectedData);
    }

    protected async Task EditNodeAsync(UnitBase node)
    {
        if (node == null) return;
        await ShowDialogAsync("Редактирование параметров узла", node, false, UnitService.UpdateAsync);
    }

    protected async Task DeleteNodeAsync()
    {
        if (Context.SelectedData == null) return;
        UnitBase selected = Context.SelectedData;
        try
        {
            await UnitService.DeleteAsync(selected.Id);
            Snackbar.Add($"Объект '{selected.Name}' успешно удален", Severity.Success);
            Context.SelectedData = null;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка удаления: {ex.Message}", Severity.Error);
        }
    }

    private async Task ShowDialogAsync(string title, UnitBase model, bool isNew, Func<UnitBase, Task> saveAction)
    {
        DialogParameters parameters = new()
        {
            { "IsNew", isNew },
            { "Model", model },
            { "OnSave", saveAction }
        };
        IDialogReference dialog = await DialogService.ShowAsync<UnitEditDialog>(title, parameters);
        await dialog.Result;
    }

    protected static string GetNodeIcon(UnitKind kind) => kind switch
    {
        UnitKind.Department => Icons.Material.Filled.Business,
        UnitKind.Production => Icons.Material.Filled.PrecisionManufacturing,
        UnitKind.Storage => Icons.Material.Filled.Warehouse,
        UnitKind.Transport => Icons.Material.Filled.LocalShipping,
        UnitKind.Position => Icons.Material.Filled.LocationOn,
        _ => Icons.Material.Filled.Circle
    };
}