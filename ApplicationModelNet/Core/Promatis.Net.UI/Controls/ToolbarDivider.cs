using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Системный элемент вертикального разделителя, размещаемый между элементами управления на панелях инструментов (тулбарах).
/// </summary>
public class ToolbarDivider : BaseUiControl
{
    private readonly string _id = "core_toolbar_divider_" + Guid.NewGuid().ToString("N");

    public override string Id => _id;

    /// <summary>
    /// Привязка к нашему зафиксированному базовому рендереру DividerRenderBase.
    /// </summary>
    public override Type ComponentType => typeof(DividerRenderBase);

    protected override Task HandleTriggerAsync(object? targetData)
    {
        // Декоративный элемент панели инструментов не выполняет логику по триггерам
        return Task.CompletedTask;
    }
}