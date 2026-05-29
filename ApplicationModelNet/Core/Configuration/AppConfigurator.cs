using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Promatis.Net.Configuration;

public class AppConfigurator : IAppConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Сканируем всё (включая AuditTrigger в проекте Data)
        services.AddDomainInfrastructure(projectPrefix: "Promatis.");

        // Регистрируем базовые сервисы
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());
        services.AddScoped<DatabaseTriggerInterceptor>();

        // ОПЦИИ: JSON настройки
        services.AddSingleton(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
    }

    public virtual void ConfigureApp(IHost app)
    {
        if (app == null) throw new ArgumentNullException(nameof(app));

        // Инициализируем глобальный Service Locator при старте приложения
        AppInfrastructure.Initialize(app.Services);

        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider.AutoRegisterTriggers();
    }
}