using Promatis.Net.UI.Components.BaseToolbarWorkspacePage;

namespace Promatis.Net.UI.Components.BaseGrid;

/// <summary>
/// Специализированный контекст действий для табличных рабочих областей (гридов).
/// Полностью интегрирован в иерархию ToolbarActionContext.
/// </summary>
/// <typeparam name="TEntity">Тип доменного объекта строки таблицы.</typeparam>
public class GridActionContext<TEntity> : ToolbarActionContext<TEntity> where TEntity : class
{
    public TEntity? SelectedItem
    {
        get => SelectedData;
        set => SelectedData = value;
    }

    public GridActionContext()
    {
        Position = ToolbarPosition.Top;
    }

    // ИСПРАВЛЕНО: Явно перехватываем изменение выделенного элемента на уровне грида,
    // чтобы форсировать перерисовку тулбара при штатном клике MudBlazor
    protected override void RecalculateButtonStates()
    {
        base.RecalculateButtonStates();
        NotifyUpdate();
    }
}