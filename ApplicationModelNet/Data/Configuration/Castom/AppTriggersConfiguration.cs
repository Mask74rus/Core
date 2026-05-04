using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain;

namespace Promatis.Net.Data;

public static class AppTriggersConfiguration
{
    public static void RegisterAppTriggers(this IServiceProvider sp)
    {
        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        // Здесь только то, что специфично для этого приложения
        triggerService.Register<DomainObject, AuditTrigger>();
    }
}