using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;

namespace Promatis.Net.Data;

public class AuditTrigger(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    JsonSerializerOptions jsonOptions)
    : IAfterSaveTrigger<DomainObject>
{
    public async Task HandleAsync(EntityChangedEventArgs<DomainObject> args)
    {
        // 1. Фильтр: логируем только те сущности, которые помечены IAudit
        // Проверка через GetType() надежнее в тестах
        if (!args.Entity.GetType().GetInterfaces().Any(i => i.Name == nameof(IAudit)))
            return;

        // 2. Формируем данные для JSON в зависимости от состояния
        object changesToSerialize = args.State switch
        {
            // Для новых записей — только то, что установили
            EntityStateChangeEnum.Added => args.Changes.Select(c => new
            {
                c.PropertyName,
                NewValue = c.CurrentValue
            }),

            // Для изменений и мягкого удаления — Old и New
            EntityStateChangeEnum.Modified or EntityStateChangeEnum.SoftDeleted => args.Changes.Select(c => new
            {
                c.PropertyName,
                OldValue = c.OriginalValue,
                NewValue = c.CurrentValue
            }),

            // Для полного удаления (если нужно логировать что удалили)
            EntityStateChangeEnum.Deleted => new { Message = "Объект удален полностью" },

            _ => args.Changes
        };

        // 3. Создаем запись лога
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = args.Entity.GetType().Name,
            EntityId = args.Entity.Id,
            // Сюда попадет строка "Added", "Modified", "Deleted" или "SoftDeleted"
            Action = args.State.ToString(),
            ChangedAt = args.ChangedAt,
            ChangedBy = args.ChangedBy,
            ChangesJson = JsonSerializer.Serialize(changesToSerialize, jsonOptions)
        };

        // 4. Сохранение в БД
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Set<AuditLog>().Add(auditLog);
        await context.SaveChangesAsync();
    }
}