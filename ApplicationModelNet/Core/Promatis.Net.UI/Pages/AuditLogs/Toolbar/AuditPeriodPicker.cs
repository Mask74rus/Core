using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditPeriodPicker : BaseUiControl, IHasValue
{
    private readonly Action _onChanged;

    public override string Id => "audit_filter_period";
    public override Type ComponentType => typeof(DateRangeRenderBase); // Рендерер ядра
    public override string Title => "Период логов";

    public object? Value { get; set; }

    public AuditPeriodPicker(Action onChanged)
    {
        _onChanged = onChanged;
        Value = new DateRange(DateTime.Today.AddDays(-7), DateTime.Today);
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        _onChanged.Invoke(); // Период изменился — даем импульс странице
        return Task.CompletedTask;
    }
}