using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalOperationParameterBaseValidator<T, TOperation, TParameter> : DomainObjectValidator<T>
    where T : TechnologicalOperationParameterBase<TOperation, TParameter>
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
{
    protected TechnologicalOperationParameterBaseValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .WithMessage("Идентификатор операции обязателен.");

        RuleFor(x => x.ParameterId)
            .NotEmpty()
            .WithMessage("Идентификатор технологического параметра обязателен.");

        // Кросс-полевая валидация: Максимальное значение не может быть меньше минимального
        RuleFor(x => x.MaxValue)
            .GreaterThanOrEqualTo(x => x.MinValue)
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
            .WithMessage("Максимально допустимое значение не может быть меньше минимального.");
    }
}