using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class FluentValidationTrigger(IServiceScopeFactory scopeFactory) // ИСПРАВЛЕНО: Инжектируем IServiceScopeFactory вместо IServiceProvider
    : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
    public async Task HandleAsync(EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args)
    {
        Type entityType = args.Entity.GetType();
        Type validatorType = typeof(IValidator<>).MakeGenericType(entityType);

        // ИСПРАВЛЕНО: Создаем изолированную, безопасную область видимости (Scope) на время валидации.
        // Это гарантирует, что IServiceProvider будет "живым" и успешно разрешит наш GlobalPolymorphicValidator!
        using IServiceScope scope = scopeFactory.CreateScope();

        // Запрашиваем валидатор из свежего, гарантированно живого scope-провайдера
        if (scope.ServiceProvider.GetService(validatorType) is IValidator validator)
        {
            var context = new ValidationContext<object>(args.Entity);
            ValidationResult result = await validator.ValidateAsync(context);

            if (!result.IsValid)
            {
                args.Cancel = true;
                // Формируем красивое, чистое сообщение об ошибках для UI
                args.ErrorMessage = string.Join(" | ", result.Errors.Select(e => e.ErrorMessage));
            }
        }
    }
}