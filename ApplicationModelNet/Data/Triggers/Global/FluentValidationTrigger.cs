using FluentValidation;
using FluentValidation.Results;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class FluentValidationTrigger(IServiceProvider serviceProvider) : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
    public async Task HandleAsync(EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args)
    {
        // Нам НЕ нужен новый Scope. Используем текущий провайдер.
        Type entityType = args.Entity.GetType();
        Type validatorType = typeof(IValidator<>).MakeGenericType(entityType);

        // Пытаемся получить валидатор для конкретного типа сущности
        if (serviceProvider.GetService(validatorType) is IValidator validator)
        {
            var context = new ValidationContext<object>(args.Entity);
            ValidationResult result = await validator.ValidateAsync(context);

            if (!result.IsValid)
            {
                args.Cancel = true;
                args.ErrorMessage = string.Join(" | ", result.Errors.Select(e => e.ErrorMessage));
            }
        }
    }
}