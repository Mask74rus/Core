using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый валидатор связи
/// </summary>
public abstract class TechnologicalOperationUnitBaseValidator<T, TOperation> : DomainObjectValidator<T>
    where T : TechnologicalOperationUnitBase<TOperation>
    where TOperation : DomainObject, ITechnologicalOperation
{
    protected TechnologicalOperationUnitBaseValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .WithMessage("Идентификатор операции обязателен.");

        RuleFor(x => x.UnitId)
            .NotEmpty()
            .WithMessage("Идентификатор оборудования обязателен.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10)
            .WithMessage("Приоритет оборудования должен быть в диапазоне от 1 до 10.");
    }
}