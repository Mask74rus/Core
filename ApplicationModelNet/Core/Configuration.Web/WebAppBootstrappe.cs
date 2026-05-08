using Microsoft.AspNetCore.Components;

namespace Promatis.Net.Configuration.Web;

// Наследник Bootstrapper, который умеет запускать Web-сервер
public abstract class WebAppBootstrapper<TRootComponent> : AppBootstrapper
    where TRootComponent : IComponent
{
    public override void Run(string[] args)
    {
        // Снова загружаем DLL (на случай прямого запуска)
        // LoadProjectAssemblies ("Promatis.") уже вызвана в base.Run, 
        // но здесь мы используем WebApplicationBuilder.

        // ОБЯЗАТЕЛЬНО: Без этого AppDomain не увидит модули при Type.GetType
        LoadProjectAssemblies("Promatis.");

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        List<IAppConfigurator> configs = GetConfigurators(builder.Configuration).ToList();

        foreach (IAppConfigurator cfg in configs)
            cfg.ConfigureServices(builder.Services, builder.Configuration);

        WebApplication app = builder.Build();

        // Настройка Middleware модулей
        foreach (IWebAppConfigurator cfg in configs.OfType<IWebAppConfigurator>())
        {
            cfg.ConfigureMiddleware(app);
        }

        foreach (IAppConfigurator cfg in configs)
            cfg.ConfigureApp(app);

        app.Run();
    }
}