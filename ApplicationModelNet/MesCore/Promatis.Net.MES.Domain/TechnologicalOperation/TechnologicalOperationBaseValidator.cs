using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

// Валидатор операции (остается без изменений)
public abstract class TechnologicalOperationBaseValidator<T, TLink> : ReferenceTreeBaseValidator<T>
    where T : TechnologicalOperationBase
    where TLink : TechnologicalOperationUnitBase
{
    protected TechnologicalOperationBaseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Код операции обязателен.");
    }
}