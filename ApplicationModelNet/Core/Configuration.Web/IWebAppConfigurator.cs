namespace Promatis.Net.Configuration.Web;

public interface IWebAppConfigurator : IAppConfigurator
{
    void ConfigureMiddleware(WebApplication app);
}