using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using System.Reflection;
using System.Runtime.Loader;

namespace Promatis.Net.Configuration;

public static class ServiceScanningExtensions
{
    public static void AddDomainInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        // 1. ПРИНУДИТЕЛЬНАЯ ЗАГРУЗКА: Загружаем все DLL из папки запуска, которые начинаются на префикс.
        // Это решает проблему "ленивой загрузки" сборок в .NET.
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (path != null)
        {
            List<Assembly> assembliesInFolder = Directory.GetFiles(path, $"{projectPrefix}*.dll")
                .Select(file => AssemblyLoadContext.Default.LoadFromAssemblyPath(file))
                .ToList();
        }

        // Берем все загруженные сборки с префиксом
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith(projectPrefix))
            .Distinct()
            .ToArray();

        int countBefore = services.Count;

        // 2. СКАНЕР
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            // Ищем все неабстрактные классы
            .AddClasses(c => c.Where(t => !t.IsAbstract))

            // Регистрируем Валидаторы
            .AsSelfWithInterfaces()
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime()

            // Регистрируем Триггеры
            .AddClasses(c => c.AssignableToAny(typeof(IBeforeSaveTrigger<>), typeof(IAfterSaveTrigger<>)))
                .AsSelfWithInterfaces()
                .WithScopedLifetime()
        );

        // 3. ЛОГИРОВАНИЕ
        List<ServiceDescriptor> newRegistrations = services.Skip(countBefore).ToList();
        foreach (ServiceDescriptor reg in newRegistrations)
        {
            // Проверка на валидатор
            bool isValidator = reg.ServiceType.IsGenericType &&
                               reg.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>);

            if (isValidator)
            {
                // Для валидаторов ImplementationType может быть null, если регистрация идет через фабрику, 
                // поэтому проверяем и ImplementationInstance/Factory, если нужно, но обычно в Scrutor ImplementationType на месте.
                string implName = reg.ImplementationType?.Name ?? "DynamicProxy/Factory";
                Console.WriteLine($"[SCANNER] Валидатор: {implName} -> {reg.ServiceType.Name}");
            }

            // Проверка на триггер (логируем только регистрацию самого класса)
            if (reg.ServiceType == reg.ImplementationType)
            {
                bool isTrigger = reg.ServiceType.GetInterfaces().Any(i => i.IsGenericType &&
                                                                          (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                                                                           i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

                if (isTrigger) Console.WriteLine($"[SCANNER] Триггер: {reg.ImplementationType!.Name}");
            }
        }
    }

    public static void AutoRegisterTriggers(this IServiceProvider serviceProvider)
    {
        var triggerService = serviceProvider.GetRequiredService<IDatabaseTriggerService>();

        // В .NET 10 мы можем эффективно найти все зарегистрированные триггеры
        // через рефлексию по загруженным сборкам ОДИН РАЗ при старте
        IEnumerable<Type> triggerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Promatis.") == true)
            .SelectMany(s => s.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                                                    i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>))));

        MethodInfo? registerMethod = typeof(IDatabaseTriggerService).GetMethod(nameof(IDatabaseTriggerService.Register));

        foreach (Type triggerType in triggerTypes)
        {
            IEnumerable<Type> interfaces = triggerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                             i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

            foreach (Type @interface in interfaces)
            {
                Type entityType = @interface.GetGenericArguments()[0];
                MethodInfo genericRegister = registerMethod!.MakeGenericMethod(entityType, triggerType);
                genericRegister.Invoke(triggerService, null);
            }
        }
    }
}