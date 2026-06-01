using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditEntitySelect : BaseUiControl, IHasValue, IHasOptions
{
    private readonly Action _onChanged;

    public override string Id => "audit_filter_entity";
    public override Type ComponentType => typeof(SelectRenderBase); // Рендерер ядра
    public override string Title => "Тип сущности";

    public object? Value { get; set; }
    public IEnumerable<string> Options { get; }

    public AuditEntitySelect(Action onChanged)
    {
        _onChanged = onChanged;
        Value = "Все сущности";
        Options = new List<string> { "Все сущности", "User", "Document", "Equipment" };
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        _onChanged.Invoke(); // Фильтр изменился — даем импульс странице
        return Task.CompletedTask;
    }
}