using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

public class UnitOfMeasurementValidator : ReferenceBaseValidator<UnitOfMeasurement>
{
    // Переопределяем базовую регулярку на расширенную (рус/лат, цифры, пробелы и символы °, /, *, -, %)
    protected override string CodeRegexPattern => @"^[\p{L}\p{N}_\.\°\/\*\-\s\%]*$";

    protected override string CodeValidationMessage => "Краткое обозначение может содержать буквы (рус/лат), цифры, пробелы и символы °, /, *, -, %";

    public UnitOfMeasurementValidator() : base()
    {
        // Правило NotEmpty и Matches уже отработают в базовом классе по нашему новому паттерну!

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Краткое обозначение обязательно для заполнения.")
            .MaximumLength(15).WithMessage("Краткое обозначение не должно превышать 15 символов.");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Полное наименование единицы измерения не должно превышать 100 символов.");
    }
}