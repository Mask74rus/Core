using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class GridPage<TEntity> : ComponentBase where TEntity : class
{
    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    [Parameter]
    public IEnumerable<TEntity>? Items { get; set; }

    [Parameter]
    public RenderFragment? GridColumns { get; set; }

    private TEntity? _selectedRow;

    /// <summary>
    /// Переводим в свойство. Сеттер вызывается автоматически движком MudBlazor при клике пользователя.
    /// </summary>
    protected TEntity? SelectedRow
    {
        get => _selectedRow;
        set
        {
            // КРИТИЧЕСКАЯ ЗАЩИТА: Блокируем рассинхронизацию и сброс в null при повторном клике
            if (value == null && _selectedRow != null)
            {
                return;
            }

            if (_selectedRow != value)
            {
                _selectedRow = value;

                // Передаем импульс в тулбар для активации кнопок CRUD
                if (ActionContext is IHasSelectedData<TEntity> bindableContext)
                {
                    bindableContext.SelectedData = value;
                    bindableContext.OnContextUpdated?.Invoke();
                }
            }
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext is IHasSelectedData<TEntity> bindableContext)
        {
            _selectedRow = bindableContext.SelectedData;
        }
    }
}