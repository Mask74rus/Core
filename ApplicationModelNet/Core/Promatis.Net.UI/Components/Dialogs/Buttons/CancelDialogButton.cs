using MudBlazor;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Components.Dialogs.Buttons;

public class CancelDialogButton : BaseUiControl, IDisposable
{
    private readonly IDialogActionContext _dialogContext;
    private readonly string _id = "dialog_action_cancel_" + Guid.NewGuid().ToString("N");

    public override string Id => _id;
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Отмена";

    public CancelDialogButton(IDialogActionContext dialogContext)
    {
        _dialogContext = dialogContext ?? throw new ArgumentNullException(nameof(dialogContext));
        _dialogContext.OnContextStateChanged += SyncEnabledState;

        ComponentParameters.Add("Color", Color.Default);
        ComponentParameters.Add("Variant", Variant.Text);

        IsEnabled = !_dialogContext.IsProcessing;
    }

    private void SyncEnabledState()
    {
        IsEnabled = !_dialogContext.IsProcessing;
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        _dialogContext.CloseCancel();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _dialogContext.OnContextStateChanged -= SyncEnabledState;
    }
}