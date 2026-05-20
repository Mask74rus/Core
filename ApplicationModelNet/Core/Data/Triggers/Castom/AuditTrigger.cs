using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;

namespace Promatis.Net.Data;

public class AuditTrigger(
    IServiceScopeFactory scopeFactory, 
    JsonSerializerOptions jsonOptions)
    : IAfterSaveTrigger<DomainObject>
{
    public async Task HandleAsync(EntityChangedEventArgs<DomainObject> args)
    {
        // Очищаем рантайм-тип от возможной динамической прокси-оболочки EF Core (TPT-стратегия)
        Type entityType = args.Entity.GetType();
        if (entityType.Namespace == "Castle.Proxies" && entityType.BaseType != null)
        {
            entityType = entityType.BaseType;
        }

        // 1. Фильтр: логируем только те сущности, которые помечены IAudit
        if (entityType.GetInterfaces().All(i => i.Name != nameof(IAudit)))
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
            EntityName = entityType.Name, // ИСПРАВЛЕНО: Пишем чистое имя класса оргструктуры
            EntityId = args.Entity.Id,
            Action = args.State.ToString(),
            ChangedAt = args.ChangedAt,
            ChangedBy = args.ChangedBy,
            ChangesJson = JsonSerializer.Serialize(changesToSerialize, jsonOptions)
        };

        // 4. ИСПРАВЛЕНО: Безопасное сохранение в БД через выделенный, гарантированно живой Scope
        using IServiceScope scope = scopeFactory.CreateScope();

        // Ищем фабрику базового контекста через провайдер локального Scope
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();

        if (factory != null)
        {
            await using ApplicationDbContext context = await factory.CreateDbContextAsync();
            context.Set<AuditLog>().Add(auditLog);
            await context.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine("[AuditTrigger][Error] Не удалось найти IDbContextFactory<ApplicationDbContext>");
        }
    }
}