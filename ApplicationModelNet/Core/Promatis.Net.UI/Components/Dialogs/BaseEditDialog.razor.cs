using FluentValidation;
using Promatis.Net.Domain;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain.Interface;
using Severity = MudBlazor.Severity;


namespace Promatis.Net.UI.Components.Dialogs;

public partial class BaseEditDialog : ComponentBase
{
    protected MudForm _form = null!;
    protected bool _isProcessing;

    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Title { get; set; } = "Редактирование";
    [Parameter] public bool IsNew { get; set; }
    [Parameter] public object Model { get; set; } = null!;

    // Используем базовый интерфейс FluentValidation, у которого нет дженерик-конфликтов
    [Parameter] public IValidator Validator { get; set; } = null!;

    [Parameter] public RenderFragment? FormContent { get; set; }
    [Parameter] public Func<Task> OnSaveAction { get; set; } = null!;
    [Parameter] public ISnackbar Snackbar { get; set; } = null!;

    protected void Cancel()
    {
        MudDialog.Cancel();
    }

    protected async Task Submit()
    {
        await _form.ValidateAsync();

        if (!_form.IsValid)
        {
            Snackbar.Add("Пожалуйста, исправьте ошибки в форме перед сохранением.", Severity.Warning);
            return;
        }

        _isProcessing = true;
        StateHasChanged();

        try
        {
            if (OnSaveAction != null)
            {
                await OnSaveAction();
            }
            MudDialog.Close(DialogResult.Ok(Model));
        }
        catch (Exception ex)
        {
            string message = ex.InnerException?.Message ?? ex.Message;
            Snackbar.Add($"Ошибка чтения: {message}", Severity.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
}