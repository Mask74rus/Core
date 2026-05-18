using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.UI;

/// <summary>
/// Универсальный базовый компонент для отображения любых древовидных структур справочников Promatis.
/// </summary>
/// <typeparam name="TEntity">Сущность, наследуемая от базового иерархического класса ReferenceTreeBase.</typeparam>

public abstract class BaseTreeView<TEntity> : ComponentBase, IDisposable
    where TEntity : ReferenceTreeBase
{
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public string Title { get; set; } = "Иерархическая структура";
    [Parameter] public EventCallback<TEntity> OnNodeSelected { get; set; }

    protected IReferenceTreeService<TEntity> TreeService { get; private set; } = null!;

    // ИСПРАВЛЕНО: Используем нативный класс MudBlazor v9 для хранения элементов дерева
    protected List<TreeItemData<TreeRowModel<TEntity>>> RootItems { get; set; } = new();
    protected bool IsLoadingRoot;

    protected readonly CancellationTokenSource Cts = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        var serviceType = typeof(IReferenceTreeService<>).MakeGenericType(typeof(TEntity));
        TreeService = (IReferenceTreeService<TEntity>)ServiceProvider.GetRequiredService(serviceType);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadRootElementsAsync();
        }
    }

    private async Task LoadRootElementsAsync()
    {
        IsLoadingRoot = true;
        StateHasChanged();

        try
        {
            var roots = await TreeService.GetRootsAsync();

            // ИСПРАВЛЕНО: Оборачиваем в TreeItemData и инициализируем Children пустой коллекцией,
            // чтобы MudTreeView знал, что узлы потенциально имеют детей и их можно раскрывать (ленивая загрузка)
            RootItems = roots
                .Select(x => new TreeItemData<TreeRowModel<TEntity>>
                {
                    Value = new TreeRowModel<TEntity>(x),
                    Children = new List<TreeItemData<TreeRowModel<TEntity>>>()
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка инициализации корней дерева: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoadingRoot = false;
            StateHasChanged();
        }
    }

    // ФИНАЛЬНОЕ ИСПРАВЛЕНИЕ: Сигнатура строго соответствует делегату MudBlazor v9:
    // Аргумент: Сам тип T (TreeRowModel<TEntity>)
    // Возврат: Коллекция оберток (IReadOnlyCollection<TreeItemData<TreeRowModel<TEntity>>>)
    protected async Task<IReadOnlyCollection<TreeItemData<TreeRowModel<TEntity>>>> LoadTreeNodesAsync(
        TreeRowModel<TEntity> parentNode)
    {
        if (parentNode == null)
        {
            return Array.Empty<TreeItemData<TreeRowModel<TEntity>>>();
        }

        try
        {
            // Запрашиваем дочерние элементы из вашего сервиса по ParentId
            var dbChildren = await TreeService.GetChildrenAsync(parentNode.Id);

            // Преобразуем в List и возвращаем как IReadOnlyCollection
            return dbChildren
                .Select(x => new TreeItemData<TreeRowModel<TEntity>>
                {
                    Value = new TreeRowModel<TEntity>(x),
                    Children = new List<TreeItemData<TreeRowModel<TEntity>>>() // Для ленивой загрузки
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка ленивой загрузки: {ex.Message}", Severity.Error);
            return Array.Empty<TreeItemData<TreeRowModel<TEntity>>>();
        }
    }

    // ИСПРАВЛЕНО: Так как SelectedValueChanged у MudTreeView теперь возвращает саму модель T (TreeRowModel),
    // сигнатура метода-перехватчика остается чистой и удобной
    protected async Task OnNodeSelectedInternal(TreeRowModel<TEntity> node)
    {
        if (node == null) return;

        try
        {
            var fullDomainEntity = await TreeService.GetByIdAsync(node.Id);

            if (fullDomainEntity != null && OnNodeSelected.HasDelegate)
            {
                await OnNodeSelected.InvokeAsync(fullDomainEntity);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка загрузки объекта: {ex.Message}", Severity.Error);
        }
    }

    public void Dispose()
    {
        Cts.Cancel();
        Cts.Dispose();
    }
}