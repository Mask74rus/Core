using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditEntitySelect : BaseUiControl, IHasValue, IHasOptions
{
    private readonly Action _onChanged;

    public override string Id => "audit_filter_entity";
    public override Type ComponentType => typeof(SelectRenderBase);
    public override string Title => "Тип сущности";

    public object? Value { get; set; }
    public IEnumerable<string> Options { get; set; }

    public AuditEntitySelect(List<string> availableEntities, Action onChanged)
    {
        _onChanged = onChanged;
        Value = "Все сущности";

        // Формируем динамический список опций на основе данных из БД
        var list = new List<string> { "Все сущности" };
        list.AddRange(availableEntities);
        Options = list;
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        _onChanged.Invoke();
        return Task.CompletedTask;
    }
}