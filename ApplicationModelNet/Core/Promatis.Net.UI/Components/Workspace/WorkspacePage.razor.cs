using Microsoft.AspNetCore.Components;
using Promatis.Net.Data;

namespace Promatis.Net.UI.Components.Workspace;

public partial class WorkspacePage : ComponentBase, IDisposable
{
    /// <summary>
    /// Ловим контекст текущей рабочей области из каскада.
    /// Если страница кастомная и контекст не нужен, холст отрендерится с дефолтной геометрией.
    /// </summary>
    [CascadingParameter] protected WorkspaceActionContext? ActionContext { get; set; }

    [Parameter] public RenderFragment? BodyContent { get; set; }
    [Parameter] public RenderFragment? TopContent { get; set; }
    [Parameter] public RenderFragment? BottomContent { get; set; }
    [Parameter] public RenderFragment? LeftContent { get; set; }
    [Parameter] public RenderFragment? RightContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Подписываем верхний этаж каркаса на глобальные транзакции СУБД
        DatabaseTriggerService.OnEntityCommitted += HandleDatabaseChange;
    }

    /// <summary>
    /// Принимает сигнал коммита из перехватчика и передает его в контекст текущей формы
    /// </summary>
    private void HandleDatabaseChange(EntityStateChangeEnum state, object entity)
    {
        if (ActionContext == null) return;

        // Принудительно маршалируем вызов из потока СУБД в главный UI-поток Blazor!
        // Это гарантирует, что StateHasChanged() внутри контекста намертво обновит картинку
        InvokeAsync(() =>
        {
            ActionContext.HandleGlobalEntityCommit(state, entity);
        });
    }

    /// <summary>
    /// Полное уничтожение связи с триггерами при закрытии MDI-вкладки (Защита от утечек)
    /// </summary>
    public void Dispose()
    {
        DatabaseTriggerService.OnEntityCommitted -= HandleDatabaseChange;
    }

    // --- МЕТОДЫ РАСЧЕТА ВИЗУАЛЬНОГО СТИЛЯ ПОДЛОЖКИ ---
    protected int GetPaperElevation() => ActionContext?.PaperElevation ?? 1;
    protected string GetPaperClass() => ActionContext?.PaperClass ?? "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";

    // --- МЕТОДЫ РАСЧЕТА ГЕОМЕТРИИ КАРКАСА ХОЛСТА ---
    protected string GetWorkspaceHeight() => ActionContext?.WorkspaceHeight ?? "100%";
    protected string GetTopHeight() => ActionContext?.TopZoneHeight ?? "auto";
    protected string GetBottomHeight() => ActionContext?.BottomZoneHeight ?? "auto";
    protected string GetLeftWidth() => ActionContext?.LeftZoneWidth ?? "250px";
    protected string GetRightWidth() => ActionContext?.RightZoneWidth ?? "300px";

    protected bool IsTopCollapsed() => ActionContext?.IsTopZoneCollapsed ?? false;
    protected bool IsBottomCollapsed() => ActionContext?.IsBottomZoneCollapsed ?? false;
    protected bool IsLeftCollapsed() => ActionContext?.IsLeftZoneCollapsed ?? false;
    protected bool IsRightCollapsed() => ActionContext?.IsRightZoneCollapsed ?? false;
}