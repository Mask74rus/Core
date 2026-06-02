using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Pages.AuditLogs.Toolbar;

public class AuditExportButton : BaseUiControl
{
    public override string Id => "audit_action_export";
    public override Type ComponentType => typeof(ButtonRenderBase); // Рендерер ядра
    public override string Title => "Выгрузить";
    public override string Icon => Icons.Material.Filled.Download;
    public override string Tooltip => "Выгрузить отфильтрованные логи в Excel";

    public AuditExportButton()
    {
        Alignment = UiControlAlignment.Right; // Сдвигаем кнопку экспорта вправо
    }

    protected override async Task HandleTriggerAsync(object? targetData)
    {
        // Имитируем долгую выгрузку — кнопка автоматически заблокируется и покажет спиннер ядра
        await Task.Delay(2000);
    }
}