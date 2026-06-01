using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class TreePage<TEntity> : ComponentBase where TEntity : class
{
    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    [Parameter] public IEnumerable<TEntity>? RootItems { get; set; }
    [Parameter] public Func<TEntity, IEnumerable<TEntity>>? ChildSelector { get; set; }
    [Parameter] public Func<TEntity, string>? TextSelector { get; set; }
    [Parameter] public Func<TEntity, string>? IconSelector { get; set; }

    // Используем чистый TEntity, так как ограничение class уже допускает null на уровне компилятора
    protected List<TreeItemData<TEntity>> TreeItems { get; set; } = new();
    protected TEntity? SelectedNode { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            SelectedNode = bindableContext.SelectedData;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (RootItems != null)
        {
            TreeItems = RootItems
                .Where(item => item != null)
                .Select(item => new TreeItemData<TEntity> { Value = item })
                .ToList();
        }
        else
        {
            TreeItems = new();
        }
    }

    protected void OnSelectedNodeChanged(TEntity? newSelection)
    {
        SelectedNode = newSelection;

        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            bindableContext.SelectedData = newSelection;
            bindableContext.OnContextUpdated?.Invoke();
        }
    }

    // Добавляем проверку на null, так как рантайм-объект в context.Value может отсутствовать
    protected string GetNodeText(TEntity? item) =>
        item == null ? string.Empty : (TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty);

    protected string GetNodeIcon(TEntity? item) =>
        item == null ? Icons.Material.Filled.Folder : (IconSelector?.Invoke(item) ?? Icons.Material.Filled.Folder);

    protected List<TreeItemData<TEntity>> GetChildTreeItemData(TEntity? item)
    {
        if (item == null || ChildSelector == null) return new();

        IEnumerable<TEntity>? children = ChildSelector.Invoke(item);
        if (children == null) return new();

        return children
            .Where(child => child != null)
            .Select(child => new TreeItemData<TEntity> { Value = child })
            .ToList();
    }
}