using FluentValidation;
using FluentValidation.Results;

namespace Promatis.Net.Domain;

/// <summary>
/// Универсальный валидатор для всех классов, наследуемых от ReferenceBase
/// </summary>
public class ReferenceBaseValidator<T> : DomainObjectValidator<T> where T : ReferenceBase
{
    public ReferenceBaseValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Наименование обязательно для заполнения")
            .MinimumLength(2).WithMessage("Наименование должно содержать минимум 2 символа")
            .MaximumLength(250).WithMessage("Превышена максимальная длина наименования (250 символов)");

        RuleFor(x => x.Code)
            .Matches(@"^[A-Z0-9_]*$")
            .WithMessage("Код может содержать только латиницу в верхнем регистре, цифры и нижнее подчеркивание");
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult? result = await ValidateAsync(ValidationContext<T>.CreateWithOptions((T)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    };
}