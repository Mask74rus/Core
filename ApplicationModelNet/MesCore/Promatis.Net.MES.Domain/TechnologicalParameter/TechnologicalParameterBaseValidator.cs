using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalParameterBaseValidator<T> : ReferenceBaseValidator<T>
    where T : TechnologicalParameterBase
{
    private static readonly string[] AllowedDataTypes = ["Numeric", "String", "Boolean", "DateTime"];

    protected TechnologicalParameterBaseValidator() : base()
    {
        // 1. Валидация единицы измерения (доступна, так как T — это TechnologicalParameterBase)
        RuleFor(x => x.UnitOfMeasurement)
            .NotNull()
            .WithMessage("Единица измерения не может быть null.")
            .MaximumLength(20)
            .WithMessage("Единица измерения не должна превышать 20 символов.");

        // 2. Валидация типа данных
        RuleFor(x => x.DataType)
            .NotEmpty()
            .WithMessage("Тип данных параметра обязателен.")
            .Must(type => AllowedDataTypes.Contains(type))
            .WithMessage($"Недопустимый тип данных. Разрешенные значения: {string.Join(", ", AllowedDataTypes)}.");
    }
}