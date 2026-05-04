using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace Promatis.Net.Configuration;

public class AppConfigurator : IAppConfigurator
{
    // Сохраняем ссылку на коллекцию для автоматики
    private IServiceCollection? _services;

    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        _services = services;

        // 1. АВТОМАТИКА: Находит ВСЕ валидаторы и ВСЕ триггеры в проектах Promatis.*
        services.AddDomainInfrastructure(projectPrefix: "Promatis.");

        // 2. ИНФРАСТРУКТУРА: Регистрируем сервис и интерцептор
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(static sp => sp.GetRequiredService<DatabaseTriggerService>());
        services.AddScoped<DatabaseTriggerInterceptor>();

        // 3. БД: Настройка фабрики
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly("Promatis.Net.Data.Init"))
                .AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        // 4. ОПЦИИ: JSON настройки
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });

        /*

        services.AddScoped<IUserProvider, WebUserProvider>();

         * // Этот класс живет в Web-проекте, где есть доступ к AuthenticationStateProvider
public class WebUserProvider(AuthenticationStateProvider authStateProvider) : IUserProvider
{
    public async Task<string?> GetCurrentUserNameAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.Name;
    }
}
         */
    }

    public virtual void ConfigureApp(IHost app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        // АВТОМАТИКА: Сама связывает все найденные триггеры с сущностями.
        // Больше не нужно вызывать RegisterDomainTriggers и RegisterAppTriggers вручную!
        scope.ServiceProvider.AutoRegisterTriggers(_services!);
    }
}