using FluentValidation;

namespace Promatis.Net.Domain;

public abstract class DomainObjectValidator<T> : AbstractValidator<T> where T : DomainObject
{
    protected DomainObjectValidator()
    {
        // Правила для всех объектов системы
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Идентификатор объекта не может быть пустым.");

        RuleFor(x => x.CreatedAt)
            .Must(date => date <= DateTime.UtcNow.AddSeconds(1))
            .WithMessage("Дата создания не может быть в будущем.");
    }
}