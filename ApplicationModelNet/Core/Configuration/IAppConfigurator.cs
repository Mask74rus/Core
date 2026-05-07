using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Promatis.Net.Configuration;

public interface IAppConfigurator
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    void ConfigureApp(IHost app);
}