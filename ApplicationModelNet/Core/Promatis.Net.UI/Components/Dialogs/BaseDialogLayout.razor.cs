using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Dialogs;

public partial class BaseDialogLayout<TModel> : ComponentBase where TModel : class
{
    protected MudForm _form = null!;

    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string Title { get; set; } = "Редактирование записи";

    [Parameter]
    public required TModel Model { get; set; }

    [Parameter]
    public IValidator<TModel>? Validator { get; set; }

    /// <summary>
    /// Контент, который будет развернут внутри формы (уникальные поля ввода или другие шаблоны типа ReferenceDialogLayout)
    /// </summary>
    [Parameter]
    public required RenderFragment ChildContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Model == null)
            throw new ArgumentNullException(nameof(Model), "Параметр Model является обязательным для BaseDialogLayout.");
    }

    public async Task HandleSubmitAsync()
    {
        if (_form == null) return;

        // Нативно сканируем всю форму силами MudBlazor
        await _form.ValidateAsync();

        if (_form.IsValid)
        {
            // Закрываем окно и возвращаем валидную модель на страницу
            MudDialog.Close(DialogResult.Ok(Model));
        }
    }

    public void HandleCancelAsync()
    {
        MudDialog.Cancel();
    }

    protected async Task<IEnumerable<string>> ValidateModelViaFluentAsync()
    {
        if (Validator == null) return Array.Empty<string>();

        var result = await Validator.ValidateAsync(Model);
        if (result.IsValid) return Array.Empty<string>();

        // Отдаем MudForm плоский список текстовых ошибок для подсветки инпутов
        return result.Errors.Select(e => e.ErrorMessage);
    }
}