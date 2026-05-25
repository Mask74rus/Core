using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактный валидатор для базового класса UnitBase.
/// </summary>
/// <typeparam name="T">Тип, наследуемый от UnitBase.</typeparam>
public abstract class UnitBaseValidator<T> : ReferenceBaseValidator<T>
    where T : UnitBase
{
    protected UnitBaseValidator() : base()
    {
        // Проверка, что в Type не пришло пустое/дефолтное значение флагов
        RuleFor(x => x.Type).NotEmpty();

        // Проверка соответствия Категории (Kind) и Типа (Type) на основе битовых масок вашего enum
        RuleFor(x => x)
            .Must(u => ((int)u.Kind & (int)u.Type) != 0)
            .WithMessage("Тип не соответствует категории.");
    }
}