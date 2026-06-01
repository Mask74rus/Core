using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.ElementRenderBase;

public partial class ButtonRenderBase : RenderBase
{
    protected async Task HandleClickAsync()
    {
        await Control.TriggerAsync(CurrentSelectedData);
    }
}