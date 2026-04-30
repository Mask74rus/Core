using FluentValidation;

namespace Promatis.Net.Domain;

public class ReferenceTreeBaseValidator<T> : ReferenceBaseValidator<T> where T : ReferenceTreeBase<T>
{
    public ReferenceTreeBaseValidator()
    {
        // Проверка на самоцитирование
        RuleFor(x => x.ParentId)
            .Must((model, parentId) => parentId != model.Id)
            .WithMessage("Объект не может быть родителем самому себе");

        // Базовая проверка формата
        RuleFor(x => x.ParentId)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentId.HasValue)
            .WithMessage("Родительский идентификатор не может быть пустым GUID");
    }
}
