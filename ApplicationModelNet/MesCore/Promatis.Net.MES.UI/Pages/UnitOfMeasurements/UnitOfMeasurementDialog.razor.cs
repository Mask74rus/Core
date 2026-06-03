using FluentValidation;
using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

public partial class UnitOfMeasurementDialog : ComponentBase
{
    /// <summary>
    /// Инжектируем строго типизированный Fluent-валидатор для единиц измерения из DI.
    /// Он автоматически пробросится в форму через параметры ReferenceDialogLayout.
    /// </summary>
    [Inject]
    protected IValidator<UnitOfMeasurement> Validator { get; set; } = null!;

    /// <summary>
    /// Живая редактируемая модель данных (чистая или клон), переданная контекстом страницы.
    /// Имя свойства "Model" строго совпадает с параметром вызова в OpenDialogWindowAsync.
    /// </summary>
    [Parameter]
    public required UnitOfMeasurement Model { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Model == null)
            throw new ArgumentNullException(nameof(Model), "Параметр Model обязан быть передан в диалог UnitOfMeasurementDialog.");
    }
}