using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class UiToolbar : ComponentBase, IDisposable
{
    /// <summary>
    /// Список полиморфных контролов для отрисовки на панели.
    /// </summary>
    [Parameter]
    public IEnumerable<IUiControl>? Controls { get; set; }

    /// <summary>
    /// Каскадный контекст холста для отслеживания смены выделения данных.
    /// </summary>
    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    /// <summary>
    /// Извлекает текущий выделенный объект для передачи в бизнес-логику контролов.
    /// </summary>
    protected object? CurrentSelectedData => GetSelectedDataFromContext();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SubscribeToSelectionChanged(true);

        if (Controls != null)
        {
            foreach (IUiControl control in Controls)
            {
                control.OnStateChanged += HandleStateChanged;
            }
        }
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    private object? GetSelectedDataFromContext()
    {
        if (ActionContext == null) return null;

        Type? interfaceType = ActionContext.GetType().GetInterface("IHasSelectedData`1");
        if (interfaceType != null)
        {
            return interfaceType.GetProperty("SelectedData")?.GetValue(ActionContext);
        }
        return null;
    }

    private void SubscribeToSelectionChanged(bool subscribe)
    {
        if (ActionContext == null) return;

        Type? interfaceType = ActionContext.GetType().GetInterface("IHasSelectedData`1");
        if (interfaceType != null)
        {
            PropertyInfo? eventInfo = interfaceType.GetProperty("OnContextUpdated");
            if (eventInfo != null)
            {
                var currentDelegate = (Action?)eventInfo.GetValue(ActionContext);
                if (subscribe)
                    currentDelegate += HandleStateChanged;
                else
                    currentDelegate -= HandleStateChanged;

                eventInfo.SetValue(ActionContext, currentDelegate);
            }
        }
    }

    public void Dispose()
    {
        SubscribeToSelectionChanged(false);

        if (Controls != null)
        {
            foreach (IUiControl control in Controls)
            {
                control.OnStateChanged -= HandleStateChanged;
            }
        }
    }
}