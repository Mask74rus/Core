using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration;
using Promatis.Net.Data;
using Promatis.Net.Domain;

namespace Promatis.Net.ApplicationModel.Console;

public class ConsoleConfigurator : AppConfigurator
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Вызываем базовую логику (регистрация триггеров, инфраструктуры)
        base.ConfigureServices(services, configuration);

        // Специфика консоли: Провайдер пользователя
        services.AddScoped<IUserProvider, ConsoleUserProvider>();

        // Специфика консоли: БД (например, InMemory для тестов)
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("ConsoleDb")
                .AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });
    }

    public override void ConfigureApp(IHost app)
    {
        // Сначала запускаем базовую активацию триггеров
        base.ConfigureApp(app);

        // Тут можно добавить тестовый запуск
        _ = RunDemo(app);
    }

    private async Task RunDemo(IHost app)
    {

    }
}