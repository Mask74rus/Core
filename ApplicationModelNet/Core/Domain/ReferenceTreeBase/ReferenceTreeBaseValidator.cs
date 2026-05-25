using FluentValidation;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Domain;

/// <summary>
/// Универсальный валидатор для всех классов, наследуемых от ReferenceTreeBase
/// </summary>
public abstract class ReferenceTreeBaseValidator<T> : ReferenceBaseValidator<T>
    where T : ReferenceBase, ITreeNode<T>
{
    protected ReferenceTreeBaseValidator() : base()
    {
        // Проверка на самоцитирование (объект не может указывать на себя как на родителя)
        RuleFor(x => x.ParentId)
            .Must((model, parentId) => parentId != model.Id)
            .WithMessage("Объект не может быть родителем самому себе");

        // Базовая проверка формата GUID
        RuleFor(x => x.ParentId)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentId.HasValue)
            .WithMessage("Родительский идентификатор не может быть пустым GUID");
    }
}
