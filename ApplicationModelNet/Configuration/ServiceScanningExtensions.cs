using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Promatis.Net.Data;
using System.Reflection;

namespace Promatis.Net.Configuration;

public static class ServiceScanningExtensions
{
    public static void AddDomainInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        // Находим все загруженные сборки, которые относятся к нашему решению
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName!.StartsWith(projectPrefix))
            .ToArray();

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            // 1. Валидаторы
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()

            // 2. Триггеры
            .AddClasses(c => c.AssignableToAny(typeof(IBeforeSaveTrigger<>), typeof(IAfterSaveTrigger<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
        );
    }

    public static void AutoRegisterTriggers(this IServiceProvider serviceProvider, IServiceCollection services)
    {
        var triggerService = serviceProvider.GetRequiredService<IDatabaseTriggerService>();
        var logger = serviceProvider.GetService<ILogger<DatabaseTriggerService>>();

        // 1. Находим все типы классов, которые реализуют интерфейсы триггеров
        IEnumerable<Type> triggerTypes = services
            .Select(sd => sd.ServiceType)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                 i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>))))
            .Distinct();

        MethodInfo? registerMethod = typeof(IDatabaseTriggerService).GetMethod(nameof(IDatabaseTriggerService.Register));

        foreach (Type triggerType in triggerTypes)
        {
            // Находим все интерфейсы триггеров, которые реализует данный класс
            IEnumerable<Type> interfaces = triggerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                             i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

            foreach (Type @interface in interfaces)
            {
                // Извлекаем TEntity из интерфейса (например, из IBeforeSaveTrigger<TEntity>)
                Type entityType = @interface.GetGenericArguments()[0];

                // Создаем массив из ДВУХ типов для метода Register<TEntity, TTrigger>
                Type[] genericArguments = { entityType, triggerType };

                // Вызываем MakeGenericMethod с полным массивом аргументов
                MethodInfo genericRegister = registerMethod!.MakeGenericMethod(genericArguments);

                genericRegister.Invoke(triggerService, null);

                logger?.LogInformation("Auto-registered trigger: {TriggerName} for entity {EntityName}",
                    triggerType.Name, entityType.Name);
            }
        }
    }
}