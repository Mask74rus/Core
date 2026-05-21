using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

public abstract class TechnologicalParameterValueBaseValidator<T, TUnit, TParameter> : DomainObjectValidator<T>
    where T : TechnologicalParameterValueBase<TUnit, TParameter>
    where TUnit : UnitBase
    where TParameter : TechnologicalParameterBase
{
    protected TechnologicalParameterValueBaseValidator()
    {
        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Идентификатор цеховой единицы обязателен.");

        RuleFor(x => x.TechnologicalParameterId)
            .NotEmpty().WithMessage("Идентификатор технологического параметра обязателен.");

        RuleFor(x => x.Value)
            .NotNull().WithMessage("Значение параметра не может быть null.")
            // Разрешаем пустую строку, так как для String-параметров это может быть валидно,
            // но убираем случайные пробелы по краям
            .Must(val => val == null || val.Trim() == val)
            .WithMessage("Значение не может начинаться или заканчиваться пробелами.");

        RuleFor(x => x.Date)
            .Must(date => date <= DateTime.UtcNow.AddSeconds(5))
            .WithMessage("Время фиксации параметра не может быть в будущем.");
    }
}