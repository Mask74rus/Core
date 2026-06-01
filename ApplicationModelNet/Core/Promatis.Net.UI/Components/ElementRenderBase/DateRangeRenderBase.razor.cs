using MudBlazor;

namespace Promatis.Net.UI.Components.ElementRenderBase;

public partial class DateRangeRenderBase : RenderBase
{
    /// <summary>
    /// Безопасно извлекает объект DateRange из модели контрола через интерфейс-расширение.
    /// </summary>
    protected DateRange? GetDateRange()
    {
        if (Control is IHasValue valueProvider)
        {
            return valueProvider.Value as DateRange;
        }
        return null;
    }

    /// <summary>
    /// Записывает новое значение выбранного диапазона дат в модель и запускает триггер.
    /// </summary>
    protected async Task HandleDateRangeChangedAsync(DateRange? newRange)
    {
        if (Control is IHasValue valueProvider)
        {
            valueProvider.Value = newRange;
            await Control.TriggerAsync(CurrentSelectedData);
        }
    }
}