
using global::Promatis.Net.MES.Domain;
using global::Promatis.Net.MES.Domain.Interface;
using global::Promatis.Net.MES.Service;
using global::Promatis.Net.UI;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Test.MDM.Domain;

namespace Promatis.Net.Test.MDM.UI.Unit;

public partial class UnitTreePage : ComponentBase
{
    [Inject] private IUnitBaseService<UnitBase> UnitService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    protected TreeActionContext Context { get; } = new();

    private List<TreeItemData<UnitBase>> _rootNodes = new();
    private UnitBase? _selectedNode;
    private bool _isInitialLoading = true;

    // ИСПРАВЛЕНО: OnInitializedAsync теперь выполняется МГНОВЕННО, 
    // не порождая никаких тяжелых ожиданий базы данных
    protected override Task OnInitializedAsync()
    {
        Context.PageTitle = "Структура предприятия и оборудования";
        Context.Position = ToolbarPosition.Top;

        // Возвращаем выполненную таску сразу, чтобы каркас формы мгновенно отрендерился
        return Task.CompletedTask;
    }

    // ИСПРАВЛЕНО: Тяжелый запрос к PostgreSQL запускается СТРОГО ПОСЛЕ того, 
    // как пользователь уже увидел открывшуюся вкладку и лоадер на экране!
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Запускаем загрузку корней дерева из базы данных
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

            // ИСПРАВЛЕНО: Убрано свойство HasChildren, возвращаем чистый объект данных
            _rootNodes = roots.Select(r => new TreeItemData<UnitBase>
            {
                Value = r
            }).ToList();
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
        if (parent == null)
            return [];

        try
        {
            List<UnitBase> children = await UnitService.GetChildrenAsync(parent.Id);

            // ИСПРАВЛЕНО: Материализуем через ToList(), чтобы вернуть корректную коллекцию для чтения
            return children.Select(c => new TreeItemData<UnitBase>
            {
                Value = c
            }).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка загрузки подчиненных узлов: {ex.Message}", Severity.Error);
            return [];
        }
    }

    private void OnNodeSelected(UnitBase? node)
    {
        _selectedNode = node;

        if (node == null)
        {
            Context.IsCreateChildEnabled = false;
            Context.IsEditNodeEnabled = false;
            Context.IsDeleteNodeEnabled = false;
        }
        else
        {
            Context.IsCreateChildEnabled = node.Kind != UnitKind.Position;
            Context.IsEditNodeEnabled = true;
            Context.IsDeleteNodeEnabled = true;
        }

        Context.NotifyUpdate();
    }

    protected async Task CreateRootNodeAsync()
    {
        // ИСПРАВЛЕНО: Задаем required свойство Type прямо в инициализаторе
        var newRoot = new DepartmentUnit
        {
            ParentId = null,
            Type = UnitType.Workshop // Инициализация обязательного свойства
        };

        DialogParameters parameters = new()
        {
            { nameof(UnitEditDialog.IsNew), true },
            { nameof(UnitEditDialog.Model), newRoot },
            { nameof(UnitEditDialog.OnSave), new Func<UnitBase, Task>(UnitService.AddAsync) }
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
        if (_selectedNode == null) return;

        // 1. Вычисляем категорию подчиненного узла
        UnitKind childKind = _selectedNode.Kind switch
        {
            UnitKind.Department => UnitKind.Production,
            UnitKind.Production => UnitKind.Position,
            UnitKind.Storage => UnitKind.Storage,
            UnitKind.Transport => UnitKind.Position,
            _ => UnitKind.Position
        };

        // 2. Инстанцируем физический C#-класс
        UnitBase childUnit = childKind switch
        {
            UnitKind.Department => new DepartmentUnit { Type = UnitType.Other, ParentId = _selectedNode.Id },
            UnitKind.Production => new ProductionUnit { Type = UnitType.Other, ParentId = _selectedNode.Id },
            UnitKind.Storage => new StorageUnit { Type = UnitType.Other, ParentId = _selectedNode.Id },
            UnitKind.Transport => new TransportUnit { Type = UnitType.Other, ParentId = _selectedNode.Id },
            UnitKind.Position => new PositionUnit { Type = UnitType.Other, ParentId = _selectedNode.Id },
            _ => throw new ArgumentOutOfRangeException(nameof(childKind), $"Неизвестная категория {childKind}")
        };

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНО: Явно зануляем навигационное свойство.
        // Передаем бэкенду ТОЛЬКО ParentId. Это заблокирует попытки EF Core пересоздать родителя в базе данных!
        childUnit.Parent = null;

        DialogParameters parameters = new()
        {
            { nameof(UnitEditDialog.IsNew), true },
            { nameof(UnitEditDialog.Model), childUnit },
            { nameof(UnitEditDialog.OnSave), new Func<UnitBase, Task>(UnitService.AddAsync) }
        };

        IDialogReference dialog = await DialogService.ShowAsync<UnitEditDialog>($"Добавление подузла для {_selectedNode.Name}", parameters);
        DialogResult? result = await dialog.Result;

        if (result is { Canceled: false })
        {
            Snackbar.Add("Подчиненный узел успешно добавлен", Severity.Success);

            _selectedNode = null;
            Context.IsCreateChildEnabled = false;
            Context.IsDeleteNodeEnabled = false;
            Context.NotifyUpdate();

            await RefreshTreeAsync();
        }
    }

    protected async Task EditSelectedNodeAsync()
    {
        if (_selectedNode == null) return;

        // Перенаправляем выполнение в наш готовый метод открытия диалога
        await EditNodeAsync(_selectedNode);
    }

    protected async Task EditNodeAsync(UnitBase node)
    {
        if (node == null) return;

        DialogParameters parameters = new()
        {
            { nameof(UnitEditDialog.IsNew), false },
            { nameof(UnitEditDialog.Model), node },
            { nameof(UnitEditDialog.OnSave), new Func<UnitBase, Task>(UnitService.UpdateAsync) }
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
        if (_selectedNode == null) return;

        try
        {
            await UnitService.DeleteAsync(_selectedNode.Id);
            Snackbar.Add($"Объект '{_selectedNode.Name}' успешно удален", Severity.Success);
            _selectedNode = null;
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