using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

// Валидатор операции (остается без изменений)
public abstract class TechnologicalOperationBaseValidator<T> : ReferenceTreeBaseValidator<T>
    where T : TechnologicalOperationBase
{
    protected TechnologicalOperationBaseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Код операции обязателен.");
    }
}