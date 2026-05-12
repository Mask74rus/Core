using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактный валидатор для базового класса UnitBase.
/// </summary>
/// <typeparam name="T">Тип, наследуемый от UnitBase.</typeparam>
public abstract class UnitBaseValidator<T> : ReferenceTreeBaseValidator<T>
    where T : UnitBase
{
    protected UnitBaseValidator()
    {
        // Проверка, что в Type не пришло совсем левое число:
        RuleFor(x => x.Type).NotEmpty(); 

        // Проверка соответствия Kind и Type
        RuleFor(x => x)
            .Must(u => ((int)u.Kind & (int)u.Type) != 0)
            .WithMessage("Тип не соответствует категории.");
    }
}