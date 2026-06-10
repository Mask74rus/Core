using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.ElementRenderBase;

public partial class ButtonRenderBase : RenderBase
{
    [Parameter] public Color Color { get; set; } = Color.Inherit;
    [Parameter] public Variant Variant { get; set; } = Variant.Text;

    /// <summary>
    /// Физика Blazor Server: Асинхронный перехватчик клика. 
    /// Делегирует исполнение абстрактной модели кнопки.
    /// </summary>
    protected async Task HandleClickAsync()
    {
        // Защита «первой линии» от случайных повторных кликов на уровне UI рантайма
        if (Control.IsRunning) return;

        try
        {
            // Запускаем триггер модели. Базовая физика BaseUiControl сама взведет IsRunning
            // и оповестит RenderBase через OnStateChanged для включения MudProgressCircular.
            await Control.TriggerAsync(CurrentSelectedData);
        }
        catch
        {
            // Глотаем или логируем ошибки выполнения команды, чтобы не уронить весь Blazor-канал (Circuit)
        }
    }
}