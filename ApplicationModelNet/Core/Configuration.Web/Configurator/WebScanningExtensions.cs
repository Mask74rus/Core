using Promatis.Net.UI;
using Promatis.Net.UI.Components;
using System.Reflection;

namespace Promatis.Net.Configuration.Web;

public static class WebInfrastructureExtensions
{
    public static void AddWebInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        Console.WriteLine("[SCANNER] Запуск прогрева сборок монолита (Только UI-модули)...");

        string binPath = AppContext.BaseDirectory;

        // ИСПРАВЛЕНО: На уровне файловой системы ищем только те dll, которые начинаются на префикс и заканчиваются на .UI.dll
        string[] assemblyFiles = Directory.GetFiles(binPath, $"{projectPrefix}*.UI.dll", SearchOption.TopDirectoryOnly);

        foreach (string file in assemblyFiles)
        {
            try
            {
                // Принудительно загружаем в AssemblyLoadContext.Default
                Assembly.LoadFrom(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCANNER] Не удалось загрузить файл сборки {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        // ИСПРАВЛЕНО: Фильтруем AppDomain, беря сборки строго с префиксом и окончанием ".UI"
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null
                        && a.FullName.StartsWith(projectPrefix)
                        && a.GetName().Name!.EndsWith(".UI"))
            .Distinct()
            .ToArray();

        Console.WriteLine($"[SCANNER] Сканирование завершено. Найдено {assemblies.Length} целевых UI-сборок.");

        Console.WriteLine();
        /*
        // АВТОМАТИЧЕСКАЯ РЕГИСТРАЦИЯ UI-КОНТЕКСТОВ ПЛАТФОРМЫ
        Console.WriteLine("[SCANNER] Регистрация C#-контекстов страниц по маркеру IWorkspaceContext...");

        // ИСПРАВЛЕНО: Маркер изменен на актуальный IWorkspaceContext (без слова Action)
        var contextTypes = assemblies
            .SelectMany(s => s.GetTypes())
            .Where(t => typeof(IWorkspaceContext).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && !t.IsInterface
                        && !t.IsGenericTypeDefinition);

        int registeredContextsCount = 0;
        foreach (Type type in contextTypes)
        {
            // 1. Регистрируем сам конкретный прикладной контекст (например, UnitOfMeasurementContext)
            services.AddTransient(type);
            registeredContextsCount++;

            Console.WriteLine($"          ├─ [КОНТЕКСТ] {type.Name}");

            // 2. Берем строго ближайший базовый тип (например, ReferenceContext<UnitOfMeasurement>)
            Type? baseType = type.BaseType;
            if (baseType != null && baseType.IsGenericType)
            {
                services.AddTransient(baseType, type);

                string genericArgs = string.Join(", ", baseType.GetGenericArguments().Select(a => a.Name));
                string baseTypeName = baseType.Name.Split('`')[0];
                Console.WriteLine($"          │  └─ Маска: {baseTypeName}<{genericArgs}>");
            }
        }
        Console.WriteLine($"[SCANNER] Успешно развернуто {registeredContextsCount} контекстов управления в DI.");

        Console.WriteLine();*/

        Console.WriteLine("[SCANNER] Регистрация UI-компонентов и модулей навигации через Scrutor...");

        // Автоматическое сканирование и регистрация интерфейсов IUiModule
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<IUiModule>().Where(t => !t.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        // Регистрация вашего агрегатора модулей
        services.AddScoped<UiModuleService>();

        // Логирование обнаруженных пунктов меню
        IEnumerable<Type> uiModuleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IUiModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (Type type in uiModuleTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IUiModule module)
                {
                    Console.WriteLine($"[SCANNER] UI Модуль: {module.Name}");
                    List<(string Title, string Href, string Icon, string? Group)> menuItems = module.GetMenuItems()?.ToList() ?? new();

                    if (!menuItems.Any())
                    {
                        Console.WriteLine("          └─ [Пустое меню]");
                        continue;
                    }

                    IEnumerable<(string Title, string Href, string Icon, string? Group)> rootItems = menuItems.Where(i => string.IsNullOrEmpty(i.Group));
                    IEnumerable<IGrouping<string?, (string Title, string Href, string Icon, string? Group)>> groupedItems = menuItems.Where(i => !string.IsNullOrEmpty(i.Group)).GroupBy(i => i.Group);

                    foreach ((string Title, string Href, string Icon, string? Group) item in rootItems)
                    {
                        Console.WriteLine($"          ├─ {item.Title} -> {item.Href}");
                    }
                    foreach (IGrouping<string?, (string Title, string Href, string Icon, string? Group)> group in groupedItems)
                    {
                        Console.WriteLine($"          ├─ [{group.Key}]");
                        List<(string Title, string Href, string Icon, string? Group)> itemsInGroup = group.ToList();
                        for (int i = 0; i < itemsInGroup.Count; i++)
                        {
                            string prefix = (i == itemsInGroup.Count - 1) ? "          │  └─" : "          │  ├─";
                            Console.WriteLine($"{prefix} {itemsInGroup[i].Title} -> {itemsInGroup[i].Href}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCANNER] Ошибка чтения меню для типа {type.Name}: {ex.Message}");
            }
        }

        Console.WriteLine("[SCANNER] Регистрация UI-компонентов успешно завершена");
        Console.WriteLine();
    }
}