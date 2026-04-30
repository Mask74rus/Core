using Microsoft.AspNetCore.Builder;

namespace Promatis.Net.Configuration;

public abstract class AppBootstrapper
{
    public void Run(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Получаем конфигуратор (логика выбора — в наследнике)
        IAppConfigurator configurator = CreateConfigurator();

        // Регистрация сервисов
        configurator.ConfigureServices(builder.Services, builder.Configuration);

        WebApplication app = builder.Build();

        // Настройка пайплайна
        configurator.ConfigureApp(app);

        app.Run();
    }

    // Метод-фабрика, который переопределит модуль
    protected abstract IAppConfigurator CreateConfigurator();
}