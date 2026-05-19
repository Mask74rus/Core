using System.Reflection;
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

        // СИСТЕМА ВКЛАДОК (MDI): Регистрируем инфраструктуру многовкладочного интерфейса
        services.AddSingleton<ComponentRegistry>();
        services.AddScoped<TabNavigationService>();

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

        // 5. Шина UI-событий
        services.AddScoped<IUiCommandBus, UiCommandBus>();
    }

    /// <summary>
    /// Настройка конвейера обработки HTTP-запросов (Middleware).
    /// </summary>
    public virtual void ConfigureMiddleware(WebApplication app)
    {
        // Базовые Middlewares для работы Web-приложения
        app.UseStaticFiles();
        app.UseAntiforgery();

        // 1. Получаем зарегистрированные UI-сборки из DI-контейнера хоста
        using IServiceScope scope = app.Services.CreateScope();
        var uiService = scope.ServiceProvider.GetRequiredService<UiModuleService>();

        Assembly[] moduleAssemblies = uiService.Modules
            .Select(m => m.GetType().Assembly)
            .Distinct()
            .ToArray();

        // ИНИЦИАЛИЗАЦИЯ КАРТЫ ВКЛАДОК: Наполняем реестр роутов компонентами из плагинов
        var componentRegistry = scope.ServiceProvider.GetRequiredService<ComponentRegistry>();
        componentRegistry.RegisterModules(moduleAssemblies);

        // 2. Регистрируем корневой компонент и сканируем страницы модулей на сервере
        app.MapRazorComponents<TRootComponent>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(moduleAssemblies);
    }

    public override void ConfigureApp(IHost app)
    {
        // Вызов базовой инициализации (авторегистрация триггеров БД через сканер)
        base.ConfigureApp(app);

        // Здесь можно добавить проверку здоровья БД или начальный Seed данных для Web-слоя
    }
}