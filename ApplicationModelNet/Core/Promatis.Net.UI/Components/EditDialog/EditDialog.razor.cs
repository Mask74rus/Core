using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Severity = MudBlazor.Severity;


namespace Promatis.Net.UI.Components.EditDialog;

public partial class EditDialog : ComponentBase
{
    protected MudForm _form = null!;
    protected bool _isProcessing;

    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Title { get; set; } = "Редактирование";
    [Parameter] public bool IsNew { get; set; }
    [Parameter] public object Model { get; set; } = null!;
    [Parameter] public IValidator Validator { get; set; } = null!;
    [Parameter] public Func<Task> OnSaveAction { get; set; } = null!;

    /// <summary>
    /// Коллекция декларативных вкладок, передаваемая из рантайма контекста страницы.
    /// </summary>
    [Parameter] public List<DialogTab> Tabs { get; set; } = [];

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected void Cancel() => MudDialog.Cancel();

    protected async Task<IEnumerable<string>> ExecuteFluentValidationAsync(object model)
    {
        if (model is not Domain.DomainObject || Validator == null) return Array.Empty<string>();

        var context = new ValidationContext<object>(model);
        ValidationResult result = await Validator.ValidateAsync(context);

        return result.IsValid ? Array.Empty<string>() : result.Errors.Select(e => e.ErrorMessage);
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
            if (OnSaveAction != null) await OnSaveAction();
            MudDialog.Close(DialogResult.Ok(Model));
        }
        catch (Exception ex)
        {
            string message = ex.InnerException?.Message ?? ex.Message;
            Snackbar.Add($"Ошибка сохранения: {message}", Severity.Error);
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }
}