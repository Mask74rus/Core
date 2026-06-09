using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public abstract partial class ReferenceTreePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Domain.ReferenceTreeBase<TEntity>, new()
{
    private bool _isDisposed;
    private bool _isFirstLoadExecuted;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Строго типизированный иерархический контекст управления этим деревом.
    /// </summary>
    protected abstract ReferenceTreeContext<TEntity> Context { get; }

    /// <summary>
    /// Локальный кэш рекурсивного графа для пассивного компонента TreePage.
    /// </summary>
    protected List<TreeItemData<TEntity>> TreeGraph { get; set; } = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // СВЯЗУЮЩИЙ МОСТ РЕАКТИВНОСТИ: Подписываемся на ЕДИНЫЙ открытый пульс ядра в одной точке
        if (Context != null)
        {
            Context.OnContextUpdated += HandleContextUpdated;
        }
    }

    /// <summary>
    /// ИНВАРИАНТ 0 мс ИНИЦИАЛИЗАЦИИ: Запускаем ленивый Pull данных строго после 
    /// того, как Blazor полностью отрисовал пустой скелет формы в браузере.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && !_isFirstLoadExecuted)
        {
            _isFirstLoadExecuted = true;
            await LoadAndBuildTreeGraphAsync();
        }
    }

    /// <summary>
    /// Асинхронный конвейер извлечения плоских данных и их безопасной сборки в иерархический граф.
    /// </summary>
    private async Task LoadAndBuildTreeGraphAsync()
    {
        if (Context == null) return;

        // Потокобезопасно тянем отфильтрованный список элементов из gRPC/ОЗУ-кэша Брокера.
        // Передаем пустой object в качестве state, как жестко зафиксировано в TreeContext.
        List<TEntity> flatList = await Context.GetDataAsync(new object());

        // Собираем рекурсивный граф TreeItemData силами скомпилированного C# без рефлексии
        TreeGraph = BuildTreeGraph(flatList);

        // Форсируем отрисовку готового дерева на экране
        StateHasChanged();
    }

    /// <summary>
    /// Универсальный алгоритм построения дерева за O(1) на базе вашего контракта ITreeNode.
    /// </summary>
    private List<TreeItemData<TEntity>> BuildTreeGraph(List<TEntity> flatList)
    {
        if (flatList == null || !flatList.Any()) return [];

        // 1. Создаем промежуточную карту, где для каждого узла сразу инициализируем мутабельный рабочий список его детей
        var nodeEntries = flatList.ToDictionary(
            x => x.Id,
            x => new {
                ItemData = new TreeItemData<TEntity> { Value = x },
                MutableChildren = new List<TreeItemData<TEntity>>()
            }
        );

        var rootNodes = new List<TreeItemData<TEntity>>();

        // 2. Распределяем узлы по родителям за один плоский проход O(N)
        foreach (var entry in nodeEntries.Values)
        {
            TEntity currentEntity = entry.ItemData.Value!;

            // Проверяем наличие родителя на основе вашего интерфейса ITreeNode
            if (currentEntity.ParentId == null || !nodeEntries.TryGetValue(currentEntity.ParentId.Value, out var parentEntry))
            {
                rootNodes.Add(entry.ItemData); // Узел корневой
            }
            else
            {
                parentEntry.MutableChildren.Add(entry.ItemData); // Узел подчиненный — пишем во временный мутабельный список
            }
        }

        // 3. Финальный штрих: Проставляем готовые списки детей в readonly-свойства TreeItemData
        foreach (var entry in nodeEntries.Values)
        {
            if (entry.MutableChildren.Any())
            {
                // Бесшовно скармливаем List в IReadOnlyCollection через set свойства Children
                entry.ItemData.Children = entry.MutableChildren;
            }
        }

        return rootNodes;
    }

    /// <summary>
    /// Диспетчер реактивности экрана. Вызывается при изменениях кнопок или мутациях ОЗУ-кэша.
    /// </summary>
    private void HandleContextUpdated()
    {
        InvokeAsync(async () =>
        {
            // Если контекст провел CRUD-операцию сохранения/удаления и очистил черновик,
            // принудительно пересобираем граф, запрашивая свежие данные из Брокера
            if (Context.DraftData == null)
            {
                await LoadAndBuildTreeGraphAsync();
            }
            else
            {
                // Иначе просто обновляем активность кнопок тулбара на экране
                StateHasChanged();
            }
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (Context != null)
        {
            Context.OnContextUpdated -= HandleContextUpdated;

            if (Context is IDisposable disposableContext)
            {
                disposableContext.Dispose();
            }
        }

        _isDisposed = true;
    }
}