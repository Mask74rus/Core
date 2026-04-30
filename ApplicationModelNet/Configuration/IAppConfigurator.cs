using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.Configuration;

public interface IAppConfigurator
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void ConfigureApp(WebApplication app);
}