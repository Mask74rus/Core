using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;

namespace Promatis.Net.Data;

public class AuditTrigger(
    IServiceProvider serviceProvider,
    JsonSerializerOptions jsonOptions)
    : IAfterSaveTrigger<DomainObject>
{
    public async Task HandleAsync(EntityChangedEventArgs<DomainObject> args)
    {
        // 1. Фильтр: логируем только те сущности, которые помечены IAudit
        if (!args.Entity.GetType().GetInterfaces().Any(i => i.Name == nameof(IAudit)))
            return;

        // 2. Формируем данные для JSON
        object changesToSerialize = args.State switch
        {
            EntityStateChangeEnum.Added => args.Changes.Select(c => new
            {
                c.PropertyName,
                NewValue = c.CurrentValue
            }),

            EntityStateChangeEnum.Modified or EntityStateChangeEnum.SoftDeleted => args.Changes.Select(c => new
            {
                c.PropertyName,
                OldValue = c.OriginalValue,
                NewValue = c.CurrentValue
            }),

            EntityStateChangeEnum.Deleted => new { Message = "Объект удален полностью" },

            _ => args.Changes
        };

        // 3. Создаем запись лога
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = args.Entity.GetType().Name,
            EntityId = args.Entity.Id,
            Action = args.State.ToString(),
            ChangedAt = args.ChangedAt,
            ChangedBy = args.ChangedBy,
            ChangesJson = JsonSerializer.Serialize(changesToSerialize, jsonOptions)
        };

        // 4. Сохранение в БД через "ленивое" получение фабрики
        // Создаем Scope, чтобы корректно получить фабрику, зарегистрированную в текущем модуле
        using var scope = serviceProvider.CreateScope();

        // Ищем фабрику базового контекста (которую мы "подменили" адаптером в MDM)
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();

        if (factory != null)
        {
            await using var context = await factory.CreateDbContextAsync();
            context.Set<AuditLog>().Add(auditLog);
            await context.SaveChangesAsync();
        }
        else
        {
            // Опционально: логирование ошибки, если база не настроена
            Console.WriteLine("[AuditTrigger][Error] Не удалось найти IDbContextFactory<ApplicationDbContext>");
        }
    }
}