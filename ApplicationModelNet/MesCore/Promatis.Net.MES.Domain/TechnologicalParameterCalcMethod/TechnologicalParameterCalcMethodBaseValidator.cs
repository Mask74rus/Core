using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalParameterCalcMethodBaseValidator<T, TUnit, TOperation, TParameter> : DomainObjectValidator<T>
    where T : TechnologicalParameterCalcMethodBase<TUnit, TOperation, TParameter>
    where TUnit : UnitBase
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
{
    protected TechnologicalParameterCalcMethodBaseValidator()
    {
        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Идентификатор цеховой единицы обязателен.");

        RuleFor(x => x.TechnologicalOperationId)
            .NotEmpty().WithMessage("Идентификатор технологической операции обязателен.");

        RuleFor(x => x.TechnologicalParameterId)
            .NotEmpty().WithMessage("Идентификатор технологического параметра обязателен.");

        // Проверяем, что метод расчета был явно выбран пользователем
        RuleFor(x => x.CalculationMethod)
            .NotEqual(CalculationMethod.None)
            .WithMessage("Необходимо выбрать корректный метод расчета параметров.");
    }
}