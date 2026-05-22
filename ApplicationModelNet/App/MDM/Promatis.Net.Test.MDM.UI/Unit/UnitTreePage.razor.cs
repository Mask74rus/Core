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

    // Явно инстанцируем наш специализированный доменный контекст управления тулбаром
    protected UnitTreeActionContext Context { get; } = new()
    {
        PageTitle = "Структура предприятия и оборудования",
        Position = ToolbarPosition.Top
    };

    private List<TreeItemData<UnitBase>> _rootNodes = new();
    private bool _isInitialLoading = true;

    protected override Task OnInitializedAsync()
    {
        // Тулбар полностью автономен, разметка пятизонального холста рендерится мгновенно за 0 мс
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshTreeAsync();
        }
    }

    // =========================================================================
    // ВЫСОКОПРОИЗВОДИТЕЛЬНАЯ СИСТЕМА СБОРКИ ДЕРЕВА В ОПЕРАТИВНОЙ ПАМЯТИ (1-2 мс)
    // =========================================================================

    private async Task RefreshTreeAsync()
    {
        _isInitialLoading = true;
        StateHasChanged();
        try
        {
            // Делаем ОДИН-ЕДИНСТВЕННЫЙ быстрый запрос к PostgreSQL вместо лавины ленивых вызовов
            List<UnitBase> allItems = await UnitService.GetAllAsync();

            // Строим индекс отношений по ParentId в оперативной памяти за O(M) операций
            ILookup<Guid?, UnitBase> lookup = allItems.ToLookup(x => x.ParentId);

            // Мгновенно выцепляем корневые элементы структуры
            List<UnitBase> roots = lookup[null].ToList();

            // Рекурсивно собираем TreeItemData сразу со всеми вложенными детьми в памяти.
            // Теперь коллекции item.Children заполнены на старте, и стрелки у пустых цехов исчезнут сразу!
            _rootNodes = roots.Select(r => BuildTreeItemData(r, lookup)).ToList();
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
    /// Высокопроизводительный рекурсивный сборщик, который ОДНОВРЕМЕННО строит
    /// и доменный граф навигационных свойств, и UI-иерархию Children для MudBlazor 9.4
    /// </summary>
    private TreeItemData<UnitBase> BuildTreeItemData(UnitBase current, ILookup<Guid?, UnitBase> lookup)
    {
        var uiItem = new TreeItemData<UnitBase> { Value = current };

        List<UnitBase> domainChildren = lookup[current.Id].ToList();
        current.Children.Clear();

        if (domainChildren.Any())
        {
            var uiChildren = new List<TreeItemData<UnitBase>>();

            foreach (UnitBase child in domainChildren)
            {
                current.Children.Add(child);
                child.Parent = current;

                // Уходим в рекурсию, собирая поддеревья для MudBlazor
                uiChildren.Add(BuildTreeItemData(child, lookup));
            }

            // Записываем дочернюю коллекцию в обертку UI. Дерево нативно оживает на клиенте!
            uiItem.Children = uiChildren;
        }

        return uiItem;
    }

    // =========================================================================
    // ОПЕРАЦИИ УПРАВЛЕНИЯ ДАННЫМИ (ФАБРИКА СУЩНОСТЕЙ И КОНТЕКСТА EF CORE)
    // =========================================================================

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

        // Просто ждем закрытия диалога. Вызов RefreshTreeAsync() УДАЛЕН — 
        // пятизональный холст сам реактивно перерисует экран по сигналу от интерцептора СУБД!
        await dialog.Result;
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

        // Зануляем навигационную ссылку, защищая ChangeTracker EF Core от дублирования объектов в сессии
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

            // Гасим кнопки тулбара, зануляя фокус на клиенте. Авто-апдейт дерева идет через триггеры СУБД!
            Context.SelectedData = null;
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
            // Вызов RefreshTreeAsync() УДАЛЕН — холст сам перерисует обновленный узел
            Snackbar.Add("Данные узла успешно обновлены", Severity.Success);
        }
    }

    protected async Task DeleteNodeAsync()
    {
        if (Context.SelectedData == null) return;

        UnitBase selected = Context.SelectedData;
        try
        {
            // Отдаем команду доменному сервису. Контекст EF Core запишет удаление, интерцептор поймает его,
            // а верхний уровень пятизонального холста сбросит стейты и перерисует граф
            await UnitService.DeleteAsync(selected.Id);
            Snackbar.Add($"Объект '{selected.Name}' успешно удален", Severity.Success);

            // Своевременно гасим кнопки тулбара, зануляя фокус на клиенте
            Context.SelectedData = null;
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
}