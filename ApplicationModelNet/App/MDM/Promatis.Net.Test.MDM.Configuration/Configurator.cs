using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration.Web;

namespace Promatis.Net.Test.MDM.Configuration;

public class Configurator : IWebAppConfigurator
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Модуль сам регистрирует свои сервисы и UI-модуль
        // Сканер внутри Core найдет всё с префиксом "Promatis.Net.MES.MDM"
    }

    public void ConfigureApp(IHost app)
    {
        // Например, проверка начальных данных для MDM
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        // Конфигурация промежуточного слоя
    }
}