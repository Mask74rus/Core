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
    /// Уникальный контент (инпуты), который будет развернут внутри формы.
    /// </summary>
    [Parameter]
    public required RenderFragment ChildContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Model == null)
            throw new ArgumentNullException(nameof(Model), "Параметр Model является обязательным для BaseDialogLayout.");
    }

    /// <summary>
    /// Обработка клика по кнопке «Сохранить». 
    /// Проверяет контракт валидации и возвращает объект наружу.
    /// </summary>
    public async Task HandleSubmitAsync()
    {
        if (_form == null) return;

        // 1. Включаем визуальную подсветку ошибок на инпутах MudBlazor
        await _form.ValidateAsync();

        // 2. ЖЕЛЕЗНАЯ ЗАЩИТА: Проверяем FluentValidation напрямую во избежание 
        // асинхронной гонки за флагом _form.IsValid.
        if (Validator != null)
        {
            var validationResult = await Validator.ValidateAsync(Model);
            if (!validationResult.IsValid)
            {
                return; // Валидация провалена, прерываем закрытие диалога.
            }
        }

        // 3. Валидация пройдена успешно. Закрываем окно и отдаем 
        // измененную модель обратно в вызвавший метод (в лямбду кнопки).
        MudDialog.Close(DialogResult.Ok(Model));
    }

    public void HandleCancelAsync()
    {
        MudDialog.Cancel();
    }

    /// <summary>
    /// Нативный асинхронный мост между инпутами MudForm и движком FluentValidation.
    /// </summary>
    protected async Task<IEnumerable<string>> ValidateModelViaFluentAsync()
    {
        if (Validator == null) return Array.Empty<string>();

        var result = await Validator.ValidateAsync(Model);
        if (result.IsValid) return Array.Empty<string>();

        // Отдаем MudForm плоский список текстовых ошибок для мгновенной подсветки инпутов
        return result.Errors.Select(e => e.ErrorMessage);
    }
}