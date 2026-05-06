using FluentValidation;
using FluentValidation.Results;

namespace Promatis.Net.Domain;

/// <summary>
/// Универсальный валидатор для всех классов, наследуемых от ReferenceBase
/// </summary>
public abstract class ReferenceBaseValidator<T> : DomainObjectValidator<T> where T : ReferenceBase
{
    protected ReferenceBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование обязательно для заполнения")
            // Проверка на пробелы в начале и конце
            .Must(name => name == null || name.Trim() == name)
            .WithMessage("Наименование не может начинаться или заканчиваться пробелами")
            .MinimumLength(2).WithMessage("Наименование должно содержать минимум 2 символа")
            .MaximumLength(250).WithMessage("Превышена максимальная длина наименования (250 символов)");

        RuleFor(x => x.Code)
            .Matches(@"^[A-Z0-9_]*$")
            .When(x => !string.IsNullOrEmpty(x.Code))
            .WithMessage("Код может содержать только латиницу в верхнем регистре, цифры и нижнее подчеркивание");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult? result = await ValidateAsync(ValidationContext<T>.CreateWithOptions((T)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}