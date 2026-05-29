using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components.Tree;

public partial class TreePage<TEntity> : ComponentBase, IDisposable
    where TEntity : class, ITreeNode<TEntity>, IDomainObjectHasKey<Guid>
{
    [CascadingParameter] protected TreeActionContext<TEntity> ActionContext { get; set; } = null!;

    [Parameter] public Func<TEntity, string>? NodeTextSelector { get; set; }

    [Inject]
    public IServiceProvider SystemServiceProvider { get; set; } = null!;

    // Коллекция оберток для MudTreeView 9.4
    protected List<TreeItemData<TEntity>> _rootTreeViewItems = [];

    // КЛЮЧЕВОЙ МОМЕНТ: Изначально выставляем в true. 
    // Страница мгновенно покажет спиннер, а меню приложения останется полностью отзывчивым!
    protected bool _isWarmingUp = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext == null)
        {
            throw new ArgumentNullException(nameof(ActionContext),
                $"Компонент {nameof(TreePage<TEntity>)} требует наличия {nameof(TreeActionContext<TEntity>)} в каскадных параметрах.");
        }

        // Магия привязки: отдаем контексту дерева провайдер текущей живой сессии Blazor
        ActionContext.ScopedProvider = SystemServiceProvider;

        ActionContext.OnContextUpdated += HandleContextUpdated;
        ActionContext.OnRefreshRequested += HandleRefreshRequested;
    }

    /// <summary>
    /// Вызывается автоматически СТРОГО ПОСЛЕ того, как Blazor отрисовал пустой каркас экрана.
    /// Полностью исключает фризы меню при переходе по вкладкам [6].
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // Запускаем прогрев асинхронно, освобождая UI-поток
            await WarmupTreeDataAsync();
        }
    }

    /// <summary>
    /// Производит безопасную фоновую загрузку данных из СУБД в ОЗУ-кэш брокера
    /// </summary>
    private async Task WarmupTreeDataAsync()
    {
        try
        {
            // Если ОЗУ-кэш брокера еще пуст, выполняем хит к сервису бэкенда
            if (ActionContext.DataBroker.IsInMemoryMode && ActionContext.DataBroker.InMemoryItems == null)
            {
                // Вызов улетает в БД, пока UI крутит легкий спиннер
                await ActionContext.InitializeInMemoryTreeAsync();
            }

            // Выносим тяжелую сборку графа связей тысяч узлов в фоновый поток ThreadPool,
            // чтобы поток отрисовки вообще не испытывал микро-задержек!
            await Task.Run(RebuildTreeGraph);
        }
        finally
        {
            _isWarmingUp = false;

            // Возвращаем управление в UI-поток для безопасной перерисовки готового дерева
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Быстро собирает плоский кэш брокера в древовидный граф TreeItemData за 1-2 мс
    /// </summary>
    private void RebuildTreeGraph()
    {
        if (!ActionContext.DataBroker.IsInMemoryMode || ActionContext.DataBroker.InMemoryItems == null)
        {
            _rootTreeViewItems = [];
            return;
        }

        List<TEntity> allItems = ActionContext.DataBroker.InMemoryItems;

        // 1. Создаем временную структуру для накопления дочерних элементов
        var childrenMap = allItems.ToDictionary(
            item => item.Id,
            _ => new List<TreeItemData<TEntity>>()
        );

        List<TreeItemData<TEntity>> roots = [];
        var rootsMap = new List<TEntity>();

        // 2. Сначала распределяем дочерние элементы по спискам в ОЗУ
        foreach (TEntity item in allItems)
        {
            // Создаем чистую обертку для текущего узла
            var currentWrapper = new TreeItemData<TEntity>
            {
                Value = item,
                Text = GetNodeText(item),
                Expanded = true
            };

            // Проверяем наличие валидного родителя
            if (item.ParentId.HasValue && item.ParentId.Value != Guid.Empty && childrenMap.ContainsKey(item.ParentId.Value))
            {
                childrenMap[item.ParentId.Value].Add(currentWrapper);
            }
            else
            {
                roots.Add(currentWrapper);
                rootsMap.Add(item);
            }
        }

        // 3. Рекурсивно или итеративно связываем дочерние списки с объектами TreeItemData.
        // Передаем списки как сформированные коллекции, чтобы MudBlazor 9.4 нативно прочитал иерархию.
        foreach (var rootWrapper in roots)
        {
            PopulateChildrenTree(rootWrapper, childrenMap);
        }

        _rootTreeViewItems = roots;
    }

    /// <summary>
    /// Вспомогательный метод для рекурсивного связывания readonly-коллекций Children в MudBlazor 9.4
    /// </summary>
    private void PopulateChildrenTree(TreeItemData<TEntity> currentWrapper, Dictionary<Guid, List<TreeItemData<TEntity>>> childrenMap)
    {
        if (currentWrapper.Value == null) return;

        Guid currentId = currentWrapper.Value.Id;

        if (childrenMap.TryGetValue(currentId, out var directChildren) && directChildren.Count > 0)
        {
            // Присваиваем готовый список — MudBlazor внутри сам посчитает Read-Only HasChildren как true
            currentWrapper.Children = directChildren;

            // Идем вглубь по цепочке графа
            foreach (var childWrapper in directChildren)
            {
                PopulateChildrenTree(childWrapper, childrenMap);
            }
        }
    }

    private void HandleContextUpdated()
    {
        StateHasChanged();
    }

    private void HandleRefreshRequested()
    {
        InvokeAsync(async () =>
        {
            // При real-time транзакциях СУБД пересчитываем граф в фоновом таске
            await Task.Run(RebuildTreeGraph);
            StateHasChanged();
        });
    }

    protected void OnSelectedNodeChanged(TEntity? domainEntity)
    {
        if (domainEntity == null && ActionContext.SelectedData != null) return;

        if (!EqualityComparer<TEntity>.Default.Equals(ActionContext.SelectedData, domainEntity))
        {
            ActionContext.SelectedData = domainEntity;
            StateHasChanged();
        }
    }

    protected string GetNodeText(TEntity node)
    {
        if (node == null) return string.Empty;
        if (NodeTextSelector != null) return NodeTextSelector(node);

        var nameProp = typeof(TEntity).GetProperty("Name");
        return nameProp?.GetValue(node)?.ToString() ?? node.Id.ToString();
    }

    public void Dispose()
    {
        if (ActionContext != null)
        {
            ActionContext.OnContextUpdated -= HandleContextUpdated;
            ActionContext.OnRefreshRequested -= HandleRefreshRequested;
        }
    }
}