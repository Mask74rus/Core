using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;

namespace Promatis.Net.Data;

public class AuditTrigger(IDbContextFactory<ApplicationDbContext> contextFactory)
    : IAfterSaveTrigger<DomainObject>
{
    public async Task HandleAsync(EntityChangedEventArgs<DomainObject> args)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        // Формируем объект для сериализации динамически
        object changesToSerialize;

        if (args.State == EntityStateChangeEnum.Added)
        {
            // Для новых записей создаем список анонимных объектов БЕЗ OldValue
            changesToSerialize = args.Changes.Select(c => new
            {
                c.PropertyName,
                NewValue = c.CurrentValue
            }).ToList();
        }
        else
        {
            // Для изменений оставляем структуру с OldValue
            changesToSerialize = args.Changes.Select(c => new
            {
                c.PropertyName,
                OldValue = c.OriginalValue,
                NewValue = c.CurrentValue
            }).ToList();
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = args.Entity.GetType().Name,
            EntityId = args.Entity.Id,
            Action = args.State.ToString(),
            ChangedAt = args.ChangedAt,
            ChangedBy = args.ChangedBy,
            // Сериализуем динамически созданный объект
            ChangesJson = JsonSerializer.Serialize(changesToSerialize)
        };

        context.Set<AuditLog>().Add(auditLog);
        await context.SaveChangesAsync();
    }
}
