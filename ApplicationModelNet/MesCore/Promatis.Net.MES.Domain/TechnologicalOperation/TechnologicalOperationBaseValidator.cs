using FluentValidation;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

// Валидатор операции (остается без изменений)
public abstract class TechnologicalOperationBaseValidator<T> : ReferenceTreeBaseValidator<T>
    where T : ReferenceTreeBase, ITechnologicalOperation
{
    protected TechnologicalOperationBaseValidator()
    {
        // Базовый ReferenceBaseValidator уже проверил общие поля (например, Id или Name)
        // Здесь мы пишем правила строго для технологических операций

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Код операции обязателен.")
            .MaximumLength(50)
            .WithMessage("Код операции не должен превышать 50 символов.");
    }
}