using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
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
        // 1. Сканируем всё (включая AuditTrigger в проекте Data)
        services.AddDomainInfrastructure(projectPrefix: "Promatis.");

        // 2. Регистрируем базовые сервисы
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());
        services.AddScoped<DatabaseTriggerInterceptor>();

        // 3. Теперь ApplicationDbContext можно регистрировать
        // Он будет лежать в Data, а Init будет использоваться только как Migration Assembly.
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsAssembly("Promatis.Net.Data.Init")) // Ссылка на проект с миграциями
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
        using var scope = app.Services.CreateScope();
        // Вызываем обновленный метод без передачи _services
        scope.ServiceProvider.AutoRegisterTriggers();
    }
}