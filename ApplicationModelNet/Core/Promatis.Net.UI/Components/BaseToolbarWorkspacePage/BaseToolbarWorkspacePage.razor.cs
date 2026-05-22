using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.BaseToolbarWorkspacePage;

public partial class BaseToolbarWorkspacePage<TEntity> : ComponentBase where TEntity : class
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ActionContext == null)
        {
            throw new ArgumentNullException(nameof(ActionContext),
                $"Компонент {nameof(BaseToolbarWorkspacePage<TEntity>)} требует обязательной передачи {nameof(ActionContext)}.");
        }

        // Связываем реактивность контекста с жизненным циклом Blazor-компонента
        ActionContext.OnContextUpdated = StateHasChanged;
    }

    protected async Task OnCreateClick()
    {
        if (OnCreateTriggered.HasDelegate) await OnCreateTriggered.InvokeAsync();
    }

    protected async Task OnCreateChildClick()
    {
        if (OnCreateChildTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnCreateChildTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnEditClick()
    {
        if (OnEditTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnEditTriggered.InvokeAsync(ActionContext.SelectedData);
    }

    protected async Task OnDeleteClick()
    {
        if (OnDeleteTriggered.HasDelegate && ActionContext.SelectedData != null)
            await OnDeleteTriggered.InvokeAsync(ActionContext.SelectedData);
    }
}