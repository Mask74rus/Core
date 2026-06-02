using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.ElementRenderBase;

public partial class ButtonRenderBase : RenderBase
{
    [Parameter] public Color Color { get; set; } = Color.Inherit;
    [Parameter] public Variant Variant { get; set; } = Variant.Text;

    protected async Task HandleClickAsync()
    {
        await Control.TriggerAsync(CurrentSelectedData);
    }
}