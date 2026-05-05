using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class DatabaseTriggerInterceptor(
    IDatabaseTriggerService triggerService,
    IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    // Хранилище для передачи данных между Saving и Saved в рамках одного потока/контекста
    private readonly ConcurrentDictionary<Guid, List<ChangeEntryModel>> _capturedChanges = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        string? userName = await GetUserNameAsync();

        // Предварительная обработка Soft Delete
        IEnumerable<EntityEntry<ISoftDeletable>> entries = eventData.Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        // Изменения должны быть до CaptureChanges
        foreach (EntityEntry<ISoftDeletable> entry in entries)
        {
            // Переключаем EF в режим обновления вместо удаления
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = userName;
        }

        // ВАЖНО для InMemory и тяжелых операций:
        eventData.Context.ChangeTracker.DetectChanges();

        // Захват изменений (теперь CaptureChanges определит SoftDelete)
        List<ChangeEntryModel> captured = CaptureChanges(eventData.Context, userName);
        _capturedChanges[eventData.Context.ContextId.InstanceId] = captured;

        // Валидация (BeforeSave)
        foreach (ChangeEntryModel item in captured)
        {
            await triggerService.ValidateAsync(item.Entity, item.State, item.Changes, eventData.Context);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context != null && _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out List<ChangeEntryModel>? entries))
        {
            foreach (ChangeEntryModel item in entries)
            {
                // Передаем захваченные метаданные в уведомления
                await triggerService.NotifyAsync(item.Entity, item.State, item.Changes, item.ChangedBy, item.ChangedAt);
            }
        }
        return result;
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        if (eventData.Context != null)
            _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out _);

        return base.SaveChangesFailedAsync(eventData, ct);
    }

    private List<ChangeEntryModel> CaptureChanges(DbContext context, string? userName)
    {
        // Захватываем всё, кроме таблицы логов
        List<EntityEntry> entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !e.Entity.GetType().Name.Contains("AuditLog"))
            .ToList();

        var result = new List<ChangeEntryModel>();

        foreach (EntityEntry e in entries)
        {
            EntityStateChangeEnum state = MapState(e.State);

            // Обработка Soft Delete
            if (e.Entity is ISoftDeletable soft)
            {
                PropertyEntry prop = e.Property(nameof(ISoftDeletable.DeletedAt));
                if (prop.IsModified && soft.DeletedAt != null) state = EntityStateChangeEnum.SoftDeleted;
            }

            List<PropertyChangeInfo> changes = new();

            if (state == EntityStateChangeEnum.Added)
            {
                changes = e.Properties
                    //.Where(p => p.CurrentValue != null)
                    .Select(p => new PropertyChangeInfo
                    {
                        PropertyName = p.Metadata.Name,
                        OriginalValue = null,
                        CurrentValue = p.CurrentValue
                    }).ToList();
            }
            else if (state is EntityStateChangeEnum.Modified or EntityStateChangeEnum.SoftDeleted)
            {
                // Используем OriginalValues из памяти — это НЕ создает запросов к БД
                PropertyValues originalValues = e.OriginalValues;

                foreach (PropertyEntry p in e.Properties.Where(p => p.IsModified))
                {
                    string propertyName = p.Metadata.Name;
                    object? originalValue = originalValues[propertyName];
                    object? currentValue = p.CurrentValue;

                    // Сравниваем значения, чтобы исключить ложные срабатывания (когда свойство помечено как IsModified, но значение то же)
                    if (!Equals(originalValue, currentValue))
                    {
                        changes.Add(new PropertyChangeInfo
                        {
                            PropertyName = propertyName,
                            OriginalValue = originalValue,
                            CurrentValue = currentValue
                        });
                    }
                }
            }

            // Пропускаем "пустые" обновления
            if (state == EntityStateChangeEnum.Modified && changes.Count == 0)
                continue;

            result.Add(new ChangeEntryModel
            {
                Entity = e.Entity,
                State = state,
                ChangedBy = userName,
                ChangedAt = DateTime.UtcNow,
                Changes = changes
            });
        }

        return result;
    }

    private async Task<string?> GetUserNameAsync()
    {
        try
        {
            // Пытаемся получить наш абстрактный провайдер
            using IServiceScope scope = serviceProvider.CreateScope();
            var userProvider = scope.ServiceProvider.GetService<IUserProvider>();

            if (userProvider != null)
            {
                return await userProvider.GetCurrentUserNameAsync();
            }
        }
        catch
        {
            // ignored
        }

        return "System";
    }

    private EntityStateChangeEnum MapState(EntityState state) => state switch
    {
        EntityState.Added => EntityStateChangeEnum.Added,
        EntityState.Deleted => EntityStateChangeEnum.Deleted,
        _ => EntityStateChangeEnum.Modified
    };

}