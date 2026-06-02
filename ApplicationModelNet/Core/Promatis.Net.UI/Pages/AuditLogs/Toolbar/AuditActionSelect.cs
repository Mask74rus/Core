using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditActionSelect : BaseUiControl, IHasValue, IHasOptions
{
    private readonly Action _onChanged;
    private readonly Dictionary<string, string> _actionMapping = new()
    {
        { "Все операции", "" },
        { "Создание", "Added" },
        { "Изменение", "Modified" },
        { "Удаление", "Deleted" },
        {"Мягкое удаление", "SoftDeleted"}
    };

    public override string Id => "audit_filter_action";
    public override Type ComponentType => typeof(SelectRenderBase);
    public override string Title => "Операция";

    public object? Value { get; set; }
    public IEnumerable<string> Options => _actionMapping.Keys;

    public AuditActionSelect(Action onChanged)
    {
        _onChanged = onChanged;
        Value = "Все операции"; // Дефолтное выбранное текстовое значение
    }

    // Метод для получения системного значения (строки для API) из выбранного текста
    public string? GetSelectedActionValue()
    {
        if (Value is string selectedText && _actionMapping.TryGetValue(selectedText, out string? sysValue))
        {
            return string.IsNullOrEmpty(sysValue) ? null : sysValue;
        }
        return null;
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        _onChanged.Invoke();
        return Task.CompletedTask;
    }
}