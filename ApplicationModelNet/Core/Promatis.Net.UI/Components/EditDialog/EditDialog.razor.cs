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
    protected readonly List<DialogTab> _collectedTabs = [];

    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Title { get; set; } = "Редактирование";
    [Parameter] public bool IsNew { get; set; }
    [Parameter] public object Model { get; set; } = null!;
    [Parameter] public IValidator Validator { get; set; } = null!;
    [Parameter] public Func<Task> OnSaveAction { get; set; } = null!;

    /// <summary>
    /// Сюда прикладной разработчик декларативно пишет теги <DialogTab>
    /// </summary>
    [Parameter] public RenderFragment? Tabs { get; set; }

    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>
    /// Метод для регистрации вкладок, вызываемый дочерними компонентами DialogTab
    /// </summary>
    public void RegisterTab(DialogTab tab)
    {
        if (!_collectedTabs.Contains(tab))
        {
            _collectedTabs.Add(tab);
            StateHasChanged();
        }
    }

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