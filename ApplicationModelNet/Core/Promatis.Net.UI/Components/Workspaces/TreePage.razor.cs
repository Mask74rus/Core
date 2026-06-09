using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class TreePage<TEntity> : ComponentBase where TEntity : class
{
    [Parameter]
    public List<TreeItemData<TEntity>> RootItems { get; set; } = [];

    [Parameter]
    public Func<TEntity, string>? TextSelector { get; set; }

    [Parameter]
    public Func<TEntity, string>? IconSelector { get; set; }

    /// <summary>
    /// Обратный мост (Callback). Сигнализирует о факте выбора узла пользователем.
    /// Прикладная страница свяжет это событие напрямую со свойством Context.SelectedData.
    /// </summary>
    [Parameter]
    public Action<TEntity?>? OnNodeSelected { get; set; }

    protected TEntity? SelectedNode { get; set; }

    protected string GetNodeText(TEntity? item) =>
        item == null ? string.Empty : (TextSelector?.Invoke(item) ?? item.ToString() ?? string.Empty);

    protected string GetNodeIcon(TEntity? item) =>
        item == null ? Icons.Material.Filled.Folder : (IconSelector?.Invoke(item) ?? Icons.Material.Filled.Folder);

    protected void OnSelectedNodeChanged(TEntity? newSelection)
    {
        // ЮВЕЛИРНЫЙ UX-ШАГ: Сохраняем фокус, если пользователь случайно кликнул мимо узлов
        if (newSelection == null && SelectedNode != null) return;

        SelectedNode = newSelection;

        // Просто декларируем факт действия координатору верхнего уровня
        OnNodeSelected?.Invoke(newSelection);
    }
}