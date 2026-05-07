using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using System.Reflection;
using System.Runtime.Loader;
using Promatis.Net.Service;

namespace Promatis.Net.Configuration;

public static class ServiceScanningExtensions
{
    public static void AddDomainInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        Console.WriteLine();
        Console.WriteLine("[SCANNER] Запуск автоматической регистрации в DI...");

        // 1. ПРИНУДИТЕЛЬНАЯ ЗАГРУЗКА
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (path != null)
        {
            foreach (string file in Directory.GetFiles(path, $"{projectPrefix}*.dll"))
            {
                try
                {
                    // Просто загружаем. Этого достаточно, чтобы сборка попала в контекст
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку загрузки конкретной DLL
                    string fileName = Path.GetFileName(file);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[SCANNER][ERROR] Не удалось загрузить сборку {fileName}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        // Берем все загруженные сборки с префиксом
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith(projectPrefix))
            .Distinct()
            .ToArray();

        int countBefore = services.Count;

        services.Scan(scan => scan
            .FromAssemblies(assemblies)

            // Регистрация СЕРВИСОВ
            // Ищем всех наследников BaseService<,> (например, OrderService)
            .AddClasses(c => c.AssignableTo(typeof(BaseService<,,>)).Where(t => !t.IsAbstract))
            .AsSelfWithInterfaces()
            .WithScopedLifetime()

            // Регистрация ВАЛИДАТОРОВ
            .AddClasses(c => c.AssignableTo(typeof(IValidator<>)).Where(t => !t.IsAbstract))
            .AsImplementedInterfaces()
            .WithTransientLifetime()

            // Регистрация ТРИГГЕРОВ
            .AddClasses(c => c.AssignableToAny(typeof(IBeforeSaveTrigger<>), typeof(IAfterSaveTrigger<>)).Where(t => !t.IsAbstract))
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
        );

        // 3. ЛОГИРОВАНИЕ
        List<ServiceDescriptor> newRegistrations = services.Skip(countBefore).ToList();
        var loggedTypes = new HashSet<string>();

        foreach (ServiceDescriptor reg in newRegistrations)
        {
            // 1. Проверка на валидатор
            bool isValidator = reg.ServiceType.IsGenericType &&
                               reg.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>);

            if (isValidator)
            {
                var inheritanceChain = new List<string>();
                Type? currentType = reg.ImplementationType;

                while (currentType != null && currentType != typeof(object))
                {
                    string typeName = currentType.Name.Contains('`')
                        ? currentType.Name.Split('`')[0]
                        : currentType.Name;

                    inheritanceChain.Add(typeName);
                    currentType = currentType.BaseType;

                    if (typeName == "AbstractValidator") break;
                }

                string chainDisplay = string.Join(" -> ", inheritanceChain);
                string logKey = $"Val:{reg.ImplementationType?.FullName}";

                if (loggedTypes.Add(logKey))
                {
                    // Теперь в консоли: CategoryValidator -> ... : IValidator<Category>
                    Console.WriteLine($"[SCANNER] Валидатор: {chainDisplay} : IValidator");
                }
            }

            // 2. Проверка на сервис (наследники BaseService)
            if (reg.ImplementationType != null && reg.ServiceType == reg.ImplementationType)
            {
                bool isService = false;
                var inheritanceChain = new List<string>();
                Type? currentType = reg.ImplementationType;

                while (currentType != null && currentType != typeof(object))
                {
                    // Очищаем имя от служебных символов `1 или `2
                    string typeName = currentType.Name.Contains('`')
                        ? currentType.Name.Split('`')[0]
                        : currentType.Name;

                    inheritanceChain.Add(typeName);

                    // Если дошли до корня BaseService<,>, значит это наш сервис
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(BaseService<,,>))
                    {
                        isService = true;
                        break;
                    }
                    currentType = currentType.BaseType;
                }

                if (isService && loggedTypes.Add($"Srv:{reg.ImplementationType.FullName}"))
                {
                    string chainDisplay = string.Join(" -> ", inheritanceChain);
                    // Выводим красивую цепочку: OrderService -> ReferenceService -> BaseService
                    Console.WriteLine($"[SCANNER] Сервис:    {chainDisplay}");
                }

                // 3. Проверка на триггер (остается без изменений)
                bool isTrigger = reg.ServiceType.GetInterfaces().Any(i => i.IsGenericType &&
                                                                          (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                                                                           i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

                if (isTrigger && loggedTypes.Add($"Tri:{reg.ImplementationType.Name}"))
                {
                    Console.WriteLine($"[SCANNER] Триггер:   {reg.ImplementationType.Name}");
                }
            }
        }
        Console.WriteLine("[SCANNER] Завершено.");
        Console.WriteLine();
    }

    public static void AutoRegisterTriggers(this IServiceProvider serviceProvider)
    {
        Console.WriteLine("[AUTOREG] Запуск автоматической регистрации триггеров в DatabaseTriggerService...");

        var triggerService = serviceProvider.GetRequiredService<IDatabaseTriggerService>();

        // 1. Поиск всех классов триггеров в сборках Promatis.*
        IEnumerable<Type> triggerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Promatis.") == true)
            .SelectMany(s => s.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                                                   (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                                                    i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>))))
            .ToList();

        MethodInfo? registerMethod = typeof(IDatabaseTriggerService).GetMethod(nameof(IDatabaseTriggerService.Register));

        int totalBindings = 0;

        foreach (Type triggerType in triggerTypes)
        {
            // Находим все интерфейсы триггеров, которые реализует данный класс
            IEnumerable<Type> interfaces = triggerType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                             i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

            foreach (Type @interface in interfaces)
            {
                // Получаем тип сущности (T из IBeforeSaveTrigger<T>)
                Type entityType = @interface.GetGenericArguments()[0];

                // Определяем тип триггера для красивого лога
                string triggerKind = @interface.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>)
                    ? "Before"
                    : "After ";

                try
                {
                    // Вызываем Register<TEntity, TTrigger>()
                    MethodInfo genericRegister = registerMethod!.MakeGenericMethod(entityType, triggerType);
                    genericRegister.Invoke(triggerService, null);

                    // ЛОГИРОВАНИЕ УСПЕШНОЙ ПРИВЯЗКИ
                    Console.WriteLine($"[AUTOREG] [{triggerKind}] {entityType.Name} <--- {triggerType.Name}");
                    totalBindings++;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[AUTOREG][ERROR] Ошибка привязки {triggerType.Name} к {entityType.Name}: {ex.InnerException?.Message ?? ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        Console.WriteLine($"[AUTOREG] Завершено. Создано привязок: {totalBindings}");
        Console.WriteLine();
    }
}