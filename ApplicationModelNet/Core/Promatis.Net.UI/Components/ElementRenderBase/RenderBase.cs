using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.ElementRenderBase;

/// <summary>
/// Абстрактный базовый компонент для всех Razor-рендереров элементов управления платформы.
/// </summary>
public abstract class RenderBase : ComponentBase, IDisposable
{
    /// <summary>
    /// Бизнес-модель элемента управления, передаваемая из DynamicComponent.
    /// </summary>
    [Parameter]
    public IUiControl Control { get; set; } = null!;

    /// <summary>
    /// Каскадный контекст холста рабочей области.
    /// </summary>
    [CascadingParameter]
    protected IWorkspaceActionContext? ActionContext { get; set; }

    /// <summary>
    /// Безопасно извлекает текущие выделенные данные из контекста формы.
    /// </summary>
    protected object? CurrentSelectedData => GetSelectedDataFromContext();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Регистрируем реактивное обновление интерфейса при смене состояния бизнес-модели
        Control.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    private object? GetSelectedDataFromContext()
    {
        if (ActionContext == null) return null;

        Type? interfaceType = ActionContext.GetType().GetInterface("IHasSelectedData`1");
        return interfaceType?.GetProperty("SelectedData")?.GetValue(ActionContext);
    }

    public virtual void Dispose() => Control.OnStateChanged -= HandleStateChanged;
}