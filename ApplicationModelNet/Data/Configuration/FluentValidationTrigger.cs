using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class FluentValidationTrigger(DatabaseTriggerService triggerService) : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
    public async Task HandleAsync(EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args)
    {
        // Создаем Scope из фабрики, которая живет в DatabaseTriggerService
        // Она имеет доступ ко всем регистрациям в тестах и приложении
        using IServiceScope scope = triggerService.ScopeFactory.CreateScope();

        Type entityType = args.Entity.GetType();
        Type validatorType = typeof(IValidator<>).MakeGenericType(entityType);

        if (scope.ServiceProvider.GetService(validatorType) is IValidator validator)
        {
            var context = new ValidationContext<object>(args.Entity);
            ValidationResult? result = await validator.ValidateAsync(context);

            if (!result.IsValid)
            {
                args.Cancel = true;
                args.ErrorMessage = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
            }
        }
    }
}