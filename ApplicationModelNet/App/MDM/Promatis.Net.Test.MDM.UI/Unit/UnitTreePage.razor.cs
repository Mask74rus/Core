using global::Promatis.Net.MES.Domain;
using global::Promatis.Net.MES.Domain.Interface;
using global::Promatis.Net.MES.Service;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Test.MDM.Domain;
using Promatis.Net.UI.Components.BaseToolbarWorkspacePage;
using Promatis.Net.UI.Components.BaseTree;

namespace Promatis.Net.Test.MDM.UI.Unit;

public partial class UnitTreePage : ComponentBase
{
    [Inject] private IUnitBaseService<UnitBase> UnitService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    // ИСПРАВЛЕНО: Явно инстанцируем наш специализированный доменный контекст
    protected UnitTreeActionContext Context { get; } = new()
    {
        PageTitle = "Структура предприятия и оборудования",
        Position = ToolbarPosition.Top
    };

    private List<TreeItemData<UnitBase>> _rootNodes = new();
    private bool _isInitialLoading = true;

    protected override Task OnInitializedAsync()
    {
        // ИСПРАВЛЕНО: Тулбар полностью автономен, в OnInitialized только возвращаем готовую таску
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshTreeAsync();
        }
    }

    private async Task RefreshTreeAsync()
    {
        _isInitialLoading = true;
        StateHasChanged();
        try
        {
            List<UnitBase> roots = await UnitService.GetRootsAsync();
            _rootNodes = roots.Select(r => new TreeItemData<UnitBase> { Value = r }).ToList();
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

    private async Task<IReadOnlyCollection<TreeItemData<UnitBase>>> LoadChildrenAsync(UnitBase parent)
    {
        if (parent == null) return Array.Empty<TreeItemData<UnitBase>>();

        try
        {
            List<UnitBase> children = await UnitService.GetChildrenAsync(parent.Id);
            return children.Select(c => new TreeItemData<UnitBase> { Value = c }).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка загрузки подчиненных узлов: {ex.Message}", Severity.Error);
            return Array.Empty<TreeItemData<UnitBase>>();
        }
    }

    // ИСПРАВЛЕНО: Метод OnNodeSelected полностью УДАЛЕН. 
    // Изменение фокуса строки идет через автоматический маппинг в Context.SelectedData напрямую!

    protected async Task CreateRootNodeAsync()
    {
        var newRoot = new DepartmentUnit
        {
            ParentId = null,
            Type = UnitType.Workshop
        };

        DialogParameters parameters = new()
        {
            { "IsNew", true },
            { "Model", newRoot },
            { "OnSave", new Func<UnitBase, Task>(UnitService.AddAsync) }
        };

        IDialogReference dialog = await DialogService.ShowAsync<UnitEditDialog>("Новый корневой элемент", parameters);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false })
        {
            Snackbar.Add("Корневой элемент структуры успешно добавлен", Severity.Success);
            await RefreshTreeAsync();
        }
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

        DialogParameters parameters = new()
        {
            { "IsNew", true },
            { "Model", childUnit },
            { "OnSave", new Func<UnitBase, Task>(UnitService.AddAsync) }
        };

        IDialogReference dialog = await DialogService.ShowAsync<UnitEditDialog>($"Добавление подузла для {selected.Name}", parameters);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false })
        {
            Snackbar.Add("Подчиненный узел успешно добавлен", Severity.Success);
            Context.SelectedData = null; // Сбрасываем фокус по правилам платформы
            await RefreshTreeAsync();
        }
    }

    protected async Task EditSelectedNodeAsync()
    {
        if (Context.SelectedData != null)
        {
            await EditNodeAsync(Context.SelectedData);
        }
    }

    protected async Task EditNodeAsync(UnitBase node)
    {
        if (node == null) return;

        DialogParameters parameters = new()
        {
            { "IsNew", false },
            { "Model", node },
            { "OnSave", new Func<UnitBase, Task>(UnitService.UpdateAsync) }
        };

        IDialogReference dialog = await DialogService.ShowAsync<UnitEditDialog>("Редактирование параметров узла", parameters);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false })
        {
            Snackbar.Add("Данные узла успешно обновлены", Severity.Success);
            await RefreshTreeAsync();
        }
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
            await RefreshTreeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка удаления: {ex.Message}", Severity.Error);
        }
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

    protected static string GetTranslate(UnitType type) => type switch
    {
        UnitType.Workshop => "Цех",
        UnitType.Section => "Участок",
        UnitType.Line => "Линия",
        UnitType.Workstation => "Рабочее место",
        UnitType.Storage => "Склад",
        UnitType.Zone => "Зона",
        UnitType.Rack => "Стеллаж",
        UnitType.Cell => "Ячейка",
        UnitType.Crane => "Кран",
        UnitType.MachineTool => "Станок",
        UnitType.Table => "Верстак",
        UnitType.Vehicle => "Транспорт",
        UnitType.Conveyor => "Транспортер",
        _ => "Прочее"
    };
}