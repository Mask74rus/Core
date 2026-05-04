using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public static class DatabaseTriggersConfiguration
{
    public static void RegisterDomainTriggers(this IServiceProvider services)
    {
        var triggerService = services.GetRequiredService<DatabaseTriggerService>();

        // --- ЯВНАЯ РЕГИСТРАЦИЯ ТРИГГЕРОВ ---

        // Валидация (BeforeSave)
        // FluentValidationTrigger будет работать для всех объектов с Guid-ключом
        triggerService.Register<IDomainObjectHasKey<Guid>, FluentValidationTrigger>();
        // ReferenceTreeParentTrigger будет работать для всех объектов с ParentId (деревьев)
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();
    }
}