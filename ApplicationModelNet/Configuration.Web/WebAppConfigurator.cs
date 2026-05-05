using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

namespace Promatis.Net.Configuration.Web;

public class WebAppConfigurator<TRootComponent> : AppConfigurator
    where TRootComponent : IComponent
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Вызываем базовую логику (БД, сканирование DLL, триггеры, сервисы, валидаторы)
        // Это гарантирует, что вся инфраструктура из Promatis.Net.Configuration поднимется
        base.ConfigureServices(services, configuration);

        // 2. Специфика Blazor Web App (Interactive Server)
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // 3. Регистрация MudBlazor
        // Пакет MudBlazor должен быть установлен в этом проекте
        services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 5000; // 5 секунд
        });

        // 4. Регистрация UserProvider для Web (Blazor)
        // Раскомментируйте, когда добавите Microsoft.AspNetCore.Components.Authorization
        // services.AddScoped<IUserProvider, WebUserProvider>();
    }

}