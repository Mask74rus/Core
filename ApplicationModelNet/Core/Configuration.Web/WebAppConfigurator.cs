using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

namespace Promatis.Net.Configuration.Web;

/// <summary>
/// Базовый конфигуратор для Web-приложений на базе Blazor и MudBlazor.
/// </summary>
public class WebAppConfigurator<TRootComponent> : AppConfigurator, IWebAppConfigurator
    where TRootComponent : IComponent
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Базовая инфраструктура (БД, Сервисы, Валидаторы, Триггеры, Сканер DLL)
        base.ConfigureServices(services, configuration);

        // 2. UI инфраструктура (Поиск IUiModule в сборках, навигация)
        services.AddWebInfrastructure(projectPrefix: "Promatis.");

        // 3. Специфика Blazor (Interactive Server)
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // 4. Регистрация и настройка MudBlazor
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 5000;
        });

        // 5. Провайдер пользователя для Web (Blazor)
        // services.AddScoped<IUserProvider, WebUserProvider>();
    }

    /// <summary>
    /// Настройка конвейера обработки HTTP-запросов (Middleware).
    /// </summary>
    public virtual void ConfigureMiddleware(WebApplication app)
    {
        // Базовые Middlewares для работы Web-приложения
        app.UseStaticFiles();
        app.UseAntiforgery();

        // Настройка маршрутизации Razor-компонентов. 
        // TRootComponent передается из Bootstrapper-а (обычно это App.razor)
        app.MapRazorComponents<TRootComponent>()
            .AddInteractiveServerRenderMode();

        // Маппинг статических ресурсов (в .NET 9+ для MudBlazor и RCL)
        // app.MapStaticAssets(); 
    }

    public override void ConfigureApp(IHost app)
    {
        // Вызов базовой инициализации (авторегистрация триггеров БД через сканер)
        base.ConfigureApp(app);

        // Здесь можно добавить проверку здоровья БД или начальный Seed данных для Web-слоя
    }
}