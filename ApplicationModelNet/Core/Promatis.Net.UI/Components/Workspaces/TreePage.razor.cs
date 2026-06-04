using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class TreePage<TEntity> : ComponentBase, IDisposable where TEntity : class
{
    private IWorkspaceContext? _currentContext;
    private bool _isFirstLoadExecuted;

    [CascadingParameter]
    protected IWorkspaceContext? ActionContext { get; set; }

    [Parameter] public Func<TEntity, IEnumerable<TEntity>>? ChildSelector { get; set; }
    [Parameter] public Func<TEntity, string>? TextSelector { get; set; }
    [Parameter] public Func<TEntity, string>? IconSelector { get; set; }

    protected List<TreeItemData<TEntity>> TreeItems { get; set; } = [];
    protected TEntity? SelectedNode { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ActionContext != _currentContext)
        {
            if (_currentContext != null)
            {
                _currentContext.OnContextStateChanged -= HandleContextStateChanged;
            }

            _currentContext = ActionContext;

            if (ActionContext != null)
            {
                ActionContext.OnContextStateChanged += HandleContextStateChanged;
            }
        }
    }

    /// <summary>
    /// ИСПРАВЛЕНО (Железное Архитектурное Правило): Первичный запрос данных запускается 
    /// СТРОГО ПОСЛЕ того, как Blazor полностью отрендерил интерфейс и каркас формы в браузере.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && !_isFirstLoadExecuted)
        {
            _isFirstLoadExecuted = true;
            await LoadTreeDataAsync();
        }
    }

    /// <summary>
    /// Извлекает асинхронный граф объектов через безопасный метод-диспетчер DataContext.
    /// Автоматически активирует MudOverlay загрузки холста на время gRPC/API-запроса.
    /// </summary>
    private async Task LoadTreeDataAsync()
    {
        // Проверяем, поддерживает ли текущий контекст работу с данными (является ли он DataContext)
        // Для этого динамически извлекаем строго типизированный метод через рефлексию или паттерн-матчинг,
        // но так как дерево работает с TreeContext, мы можем безопасно выполнить приведение.
        if (ActionContext != null)
        {
            // Используем динамическое приведение, так как TreeContext закрыт генериками, которые мы тут не знаем.
            // Но мы точно знаем, что у него есть метод GetDataAsync(object state).
            var method = ActionContext.GetType().GetMethod("GetDataAsync");
            if (method != null)
            {
                // Вызываем диспетчер данных контекста. Он сам включит крутилку загрузки IsLoading!
                var task = (Task<IReadOnlyList<TEntity>>)method.Invoke(ActionContext, [new object(), default(CancellationToken)])!;
                IReadOnlyList<TEntity> result = await task;

                // Перекладываем полученный граф в визуальное дерево MudTreeView
                if (result != null)
                {
                    TreeItems = result
                        .Where(item => item != null)
                        .Select(item => new TreeItemData<TEntity> { Value = item })
                        .ToList();

                    StateHasChanged();
                }
            }
        }
    }

    private void HandleContextStateChanged()
    {
        // Перерисовываем дерево, если в ОЗУ изменились поля, добавились узлы или сменился IsLoading
        InvokeAsync(StateHasChanged);
    }

    protected void OnSelectedNodeChanged(TEntity? newSelection)
    {
        // ИСПРАВЛЕНО: Внедрена фирменная ERP-защита от случайного сброса клика по узлу.
        // Повторный клик на выделенный узел больше не снимает фокус и не блокирует тулбар.
        if (newSelection == null && SelectedNode != null) return;

        SelectedNode = newSelection;

        // Транслируем выделение в ядро. Контекст сам синхронно запустит цепочку реактивности кнопок!
        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            bindableContext.SelectedData = newSelection;
        }
    }

    protected string GetNodeText(TEntity? item) =>
        item == null ? string.Empty : (TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty);

    protected string GetNodeIcon(TEntity? item) =>
        item == null ? Icons.Material.Filled.Folder : (IconSelector?.Invoke(item) ?? Icons.Material.Filled.Folder);

    protected List<TreeItemData<TEntity>> GetChildTreeItemData(TEntity? item)
    {
        if (item == null || ChildSelector == null) return [];

        IEnumerable<TEntity>? children = ChildSelector.Invoke(item);
        if (children == null) return [];

        return children
            .Where(child => child != null)
            .Select(child => new TreeItemData<TEntity> { Value = child })
            .ToList();
    }

    public void Dispose()
    {
        if (_currentContext != null)
        {
            _currentContext.OnContextStateChanged -= HandleContextStateChanged;
        }
    }
}