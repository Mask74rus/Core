using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.Service;

public static class ServiceRegistration
{
    public static void AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitService, UnitService>();
        // Другие сервисы...
    }
}