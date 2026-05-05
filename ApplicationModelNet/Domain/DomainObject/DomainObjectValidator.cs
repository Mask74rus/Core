using FluentValidation;

namespace Promatis.Net.Domain;

/// <summary>
/// Универсальный валидатор для всех классов, наследуемых от DomainObject
/// </summary>
public abstract class DomainObjectValidator<T> : AbstractValidator<T> where T : DomainObject
{
    protected DomainObjectValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Идентификатор объекта не может быть пустым.");

        RuleFor(x => x.CreatedAt)
            .Must(date => date <= DateTime.UtcNow.AddSeconds(1))
            .WithMessage("Дата создания не может быть в будущем.");
    }
}