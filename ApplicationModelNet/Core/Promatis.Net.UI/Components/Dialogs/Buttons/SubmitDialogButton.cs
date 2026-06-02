using MudBlazor;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Components.Dialogs.Buttons;

public class SubmitDialogButton : BaseUiControl, IDisposable
{
    private readonly IDialogActionContext _dialogContext;
    private readonly string _id = "dialog_action_submit_" + Guid.NewGuid().ToString("N");

    public override string Id => _id;
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Сохранить";

    public SubmitDialogButton(IDialogActionContext dialogContext)
    {
        _dialogContext = dialogContext ?? throw new ArgumentNullException(nameof(dialogContext));

        // Подписываемся на реактивное изменение состояния контекста
        _dialogContext.OnContextStateChanged += SyncEnabledState;

        ComponentParameters.Add("Color", Color.Primary);
        ComponentParameters.Add("Variant", Variant.Filled);

        // Задаем первичное состояние
        IsEnabled = !_dialogContext.IsProcessing;
    }

    private void SyncEnabledState()
    {
        // Меняем значение базового свойства без всяких override
        IsEnabled = !_dialogContext.IsProcessing;
    }

    protected override async Task HandleTriggerAsync(object? targetData)
    {
        // Строгая типизация без dynamic благодаря расширению интерфейса
        await _dialogContext.ExecuteSubmitAsync();
    }

    public void Dispose()
    {
        // Обязательно отписываемся, чтобы избежать утечек памяти в Blazor
        _dialogContext.OnContextStateChanged -= SyncEnabledState;
    }
}