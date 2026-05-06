using Microsoft.Extensions.Hosting;

namespace Promatis.Net.Configuration;

public abstract class AppBootstrapper
{
    public void Run(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        IAppConfigurator configurator = CreateConfigurator();

        // 1. Регистрация сервисов
        configurator.ConfigureServices(builder.Services, builder.Configuration);

        // 2. Сборка приложения
        IHost app = builder.Build();

        // 3. Настройка логики (ваша авторегистрация триггеров и т.д.)
        configurator.ConfigureApp(app);

        // 4. Запуск
        app.Run();
    }

    protected abstract IAppConfigurator CreateConfigurator();
}