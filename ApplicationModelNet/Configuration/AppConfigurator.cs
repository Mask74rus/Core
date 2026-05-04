using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Promatis.Net.Configuration;

public class AppConfigurator : IAppConfigurator
{
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Триггеры и валидация
        services.AddValidatorsFromAssemblyContaining<DomainObjectValidator<DomainObject>>();
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<DatabaseTriggerInterceptor>();

        // Триггеры приложения
        services.AddScoped<AuditTrigger>();

        // БД
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    // Указываем, что миграции лежат в новой библиотеке
                    x => x.MigrationsAssembly("Promatis.Net.Data.Init"))
                .AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });

        // Настраиваем опции один раз
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Чтобы в БД был стандартный JSON
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Экономим место, не пишем null-поля
            WriteIndented = false // Для БД лучше компактный вид без пробелов
        };

        // Регистрируем
        services.AddSingleton(jsonOptions);
    }

    public virtual void ConfigureApp(WebApplication app)
    {
        // Инициализация триггеров
        using IServiceScope scope = app.Services.CreateScope();

        // 1. Стандартная регистрация из метода расширения
        scope.ServiceProvider.RegisterDomainTriggers();

        // 2. Заряжаем триггеры приложения
        scope.ServiceProvider.RegisterAppTriggers();
    }
}