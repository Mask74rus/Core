using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditToolbarDivider : BaseUiControl
{
    private readonly string _id = "audit_toolbar_divider_" + Guid.NewGuid().ToString("N");

    public override string Id => _id;
    public override Type ComponentType => typeof(DividerRenderBase); // Используем тот же базовый рендер

    public AuditToolbarDivider()
    {
        Alignment = UiControlAlignment.Right; // Этот разделитель гарантированно справа
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        return Task.CompletedTask;
    }
}