using System.Reflection;
using Promatis.Net.UI;

namespace Promatis.Net.Configuration.Web;

public static class WebScanningExtensions
{
    public static void AddWebInfrastructure(this IServiceCollection services, string projectPrefix = "Promatis.")
    {
        // Берем уже загруженные сборки (AddDomainInfrastructure их уже подгрузил в AppDomain)
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith(projectPrefix))
            .Distinct()
            .ToArray();

        Console.WriteLine("[SCANNER] Регистрация UI-компонентов...");

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            // 1. Регистрация UI модулей (меню, маршруты)
            .AddClasses(c => c.AssignableTo<IUiModule>().Where(t => !t.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        // Регистрируем агрегатор модулей
        services.AddScoped<UiModuleService>();

        // Логируем найденные модули
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IEnumerable<IUiModule> modules = serviceProvider.GetServices<IUiModule>();
        foreach (IUiModule module in modules)
        {
            Console.WriteLine($"[SCANNER] UI Модуль:  {module.Name}");
        }
    }
}