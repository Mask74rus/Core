using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Базовый валидатор связи
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class TechnologicalOperationUnitBaseValidator<T> : DomainObjectValidator<T>
    where T : TechnologicalOperationUnitBase
{
    protected TechnologicalOperationUnitBaseValidator()
    {
        RuleFor(x => x.OperationId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Priority).InclusiveBetween(1, 10);
    }
}