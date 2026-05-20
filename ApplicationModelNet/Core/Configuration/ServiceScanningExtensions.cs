using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using System.Reflection;
using System.Runtime.Loader;

namespace Promatis.Net.Configuration;

public static class ServiceScanningExtensions
{
    public static void AddDomainInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        Console.WriteLine();
        Console.WriteLine("[SCANNER] Запуск автоматической регистрации в DI...");

        // 1. ПРИНУДИТЕЛЬНАЯ ЗАГРУЗКА СБОРОК
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (path != null)
        {
            foreach (string file in Directory.GetFiles(path, $"{projectPrefix}*.dll"))
            {
                try
                {
                    AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                }
                catch (Exception ex)
                {
                    string fileName = Path.GetFileName(file);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[SCANNER][ERROR] Не удалось загрузить сборку {fileName}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        // Берем все загруженные сборки с вашим системным префиксом
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith(projectPrefix))
            .Distinct()
            .ToArray();

        int countBefore = services.Count;

        // 2. АВТОМАТИЧЕСКОЕ СКАНИРОВАНИЕ И РЕГИСТРАЦИЯ ЧЕРЕЗ SCRUTOR
        services.Scan(scan =>
        {
            // Локальная функция для рекурсивной проверки всей цепочки наследования открытых generic-типов
            bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
            {
                while (toCheck != null && toCheck != typeof(object))
                {
                    Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (generic == cur) return true;
                    toCheck = toCheck.BaseType;
                }
                return false;
            }

            scan.FromAssemblies(assemblies)
                // ИСПРАВЛЕНО: Регистрация СЕРВИСОВ с поддержкой глубокого наследования (BaseService<,,>)
                .AddClasses(classes => classes.Where(type =>
                    !type.IsAbstract &&
                    !type.IsGenericTypeDefinition &&
                    IsSubclassOfRawGeneric(typeof(BaseService<,,>), type)))
                .AsSelf()
                .AsImplementedInterfaces()
                .WithScopedLifetime()

                // Регистрация ВАЛИДАТОРОВ (FluentValidation)
                .AddClasses(c => c.AssignableTo(typeof(IValidator<>))
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition))
                .AsImplementedInterfaces()
                .WithTransientLifetime()

                // Регистрация ТРИГГЕРОВ (Before/After Save)
                .AddClasses(c => c.AssignableToAny(typeof(IBeforeSaveTrigger<>), typeof(IAfterSaveTrigger<>)).Where(t => !t.IsAbstract))
                .AsSelfWithInterfaces()
                .WithScopedLifetime();
        });

        // --- РЕГИСТРАЦИЯ ГЛОБАЛЬНОГО ПОЛИМОРФНОГО ВАЛИДАТОРА ЯДРА ---
        // Регистрируем как открытый дженерик. Теперь при запросе IValidator<ЛюбойБазовыйКласс> 
        // DI-контейнер .NET 10 гарантированно отдаст наш полиморфный диспетчер
        services.AddScoped(typeof(IValidator<>), typeof(GlobalPolymorphicValidator<>));

        // 3. ИСПРАВЛЕННОЕ И НАДЕЖНОЕ ЛОГИРОВАНИЕ РЕЗУЛЬТАТОВ
        List<ServiceDescriptor> newRegistrations = services.Skip(countBefore).ToList();
        var loggedTypes = new HashSet<string>();

        foreach (ServiceDescriptor reg in newRegistrations)
        {
            // Берем реальный тип реализации (в приоритете из ImplementationType, иначе из инстанса/фабрики)
            Type? implType = reg.ImplementationType ?? reg.ImplementationInstance?.GetType();

            if (implType == null) continue;

            // --- АНАЛИЗ ВАЛИДАТОРОВ ---
            bool isValidator = reg.ServiceType.IsGenericType &&
                               reg.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>);

            if (isValidator && loggedTypes.Add($"Val:{implType.FullName}"))
            {
                var inheritanceChain = new List<string>();
                Type? currentType = implType;

                while (currentType != null && currentType != typeof(object))
                {
                    string typeName = currentType.Name.Contains('`') ? currentType.Name.Split('`')[0] : currentType.Name;
                    inheritanceChain.Add(typeName);
                    currentType = currentType.BaseType;
                    if (typeName == "AbstractValidator") break;
                }

                string chainDisplay = string.Join(" -> ", inheritanceChain);
                Console.WriteLine($"[SCANNER] Валидатор: {chainDisplay} : IValidator");
                continue; // Лог для этой записи выведен, идем к следующей
            }

            // --- АНАЛИЗ СЕРВИСОВ (Наследников BaseService) ---
            // ИСПРАВЛЕНО: Убрано жесткое условие reg.ServiceType == reg.ImplementationType, 
            // так как сервисы теперь регистрируются парами Класс + Интерфейс
            bool isService = false;
            var serviceChain = new List<string>();
            Type? currentServiceType = implType;

            while (currentServiceType != null && currentServiceType != typeof(object))
            {
                string typeName = currentServiceType.Name.Contains('`') ? currentServiceType.Name.Split('`')[0] : currentServiceType.Name;
                serviceChain.Add(typeName);

                if (currentServiceType.IsGenericType && currentServiceType.GetGenericTypeDefinition() == typeof(BaseService<,,>))
                {
                    isService = true;
                    break;
                }
                currentServiceType = currentServiceType.BaseType;
            }

            if (isService && loggedTypes.Add($"Srv:{implType.FullName}"))
            {
                string chainDisplay = string.Join(" -> ", serviceChain);
                Console.WriteLine($"[SCANNER] Сервис:    {chainDisplay}");
                continue;
            }

            // --- АНАЛИЗ ТРИГГЕРОВ ---
            bool isTrigger = implType.GetInterfaces().Any(i => i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(IBeforeSaveTrigger<>) ||
                 i.GetGenericTypeDefinition() == typeof(IAfterSaveTrigger<>)));

            if (isTrigger && loggedTypes.Add($"Tri:{implType.FullName}"))
            {
                Console.WriteLine($"[SCANNER] Триггер:   {implType.Name}");
            }
        }

        Console.WriteLine("[SCANNER] Автоматическая регистрация успешно завершена.");
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