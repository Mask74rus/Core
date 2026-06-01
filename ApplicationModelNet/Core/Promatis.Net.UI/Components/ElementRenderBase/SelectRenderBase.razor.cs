namespace Promatis.Net.UI.Components.ElementRenderBase;

public partial class SelectRenderBase : RenderBase
{
    protected string GetControlValue()
    {
        if (Control is IHasValue valueProvider && valueProvider.Value != null)
        {
            return valueProvider.Value.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    protected async Task HandleValueChangedAsync(string newValue)
    {
        if (Control is IHasValue valueProvider)
        {
            valueProvider.Value = newValue;
            await Control.TriggerAsync(CurrentSelectedData);
        }
    }

    protected IEnumerable<string> GetOptions()
    {
        if (Control is IHasOptions optionsProvider)
        {
            return optionsProvider.Options;
        }
        return Array.Empty<string>();
    }
}