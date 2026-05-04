using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;

namespace Promatis.Net.ApplicationModel.Console;

public class ConsoleConfigurator : AppConfigurator
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Вызываем базовую логику (регистрация триггеров, инфраструктуры)
        base.ConfigureServices(services, configuration);

        // Специфика консоли: Провайдер пользователя
        services.AddScoped<IUserProvider, ConsoleUserProvider>();
    }

    public override void ConfigureApp(IHost app)
    {
        // Сначала запускаем базовую активацию триггеров
        base.ConfigureApp(app);

        // Тут можно добавить тестовый запуск
        RunDemo(app);
    }

    private void RunDemo(IHost app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        // Явно указываем System.Console, если есть конфликт имен
        System.Console.WriteLine("\n=== Система запущена в консольном режиме ===");

        // Проверим, зарегистрированы ли триггеры
        //var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();
        System.Console.WriteLine("Сервис триггеров готов к работе.");
    }
}