using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class DatabaseTriggerInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    private readonly ConcurrentDictionary<Guid, List<ChangeEntryModel>> _capturedChanges = new();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        // ИСПРАВЛЕНО: Создаем локальный гарантированно живой Scope для фазыSaving
        using IServiceScope scope = scopeFactory.CreateScope();

        // Разрешаем зависимости из свежего, изолированного контейнера
        var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();
        string? userName = await GetUserNameInternalAsync(scope.ServiceProvider);

        // Предварительная обработка Soft Delete
        IEnumerable<EntityEntry<ISoftDeletable>> entries = eventData.Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<ISoftDeletable> entry in entries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = userName;
        }

        eventData.Context.ChangeTracker.DetectChanges();

        List<ChangeEntryModel> captured = CaptureChanges(eventData.Context, userName);
        _capturedChanges[eventData.Context.ContextId.InstanceId] = captured;

        // Валидация (BeforeSave) через живой triggerService
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
            // ИСПРАВЛЕНО: Создаем локальный гарантированно живой Scope для фазы Saved
            using IServiceScope scope = scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();

            foreach (ChangeEntryModel item in entries)
            {
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
        List<EntityEntry> entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !e.Entity.GetType().Name.Contains("AuditLog"))
            .ToList();

        var result = new List<ChangeEntryModel>();

        foreach (EntityEntry e in entries)
        {
            EntityStateChangeEnum state = MapState(e.State);

            if (e.Entity is ISoftDeletable soft)
            {
                PropertyEntry prop = e.Property(nameof(ISoftDeletable.DeletedAt));
                if (prop.IsModified && soft.DeletedAt != null) state = EntityStateChangeEnum.SoftDeleted;
            }

            List<PropertyChangeInfo> changes = new();

            if (state == EntityStateChangeEnum.Added)
            {
                changes = e.Properties
                    .Select(p => new PropertyChangeInfo
                    {
                        PropertyName = p.Metadata.Name,
                        OriginalValue = null,
                        CurrentValue = p.CurrentValue
                    }).ToList();
            }
            else if (state is EntityStateChangeEnum.Modified or EntityStateChangeEnum.SoftDeleted)
            {
                PropertyValues originalValues = e.OriginalValues;

                foreach (PropertyEntry p in e.Properties.Where(p => p.IsModified))
                {
                    string propertyName = p.Metadata.Name;
                    object? originalValue = originalValues[propertyName];
                    object? currentValue = p.CurrentValue;

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

    // ИСПРАВЛЕНО: Принимаем уже развернутый провайдер живого контекста
    private async Task<string?> GetUserNameInternalAsync(IServiceProvider provider)
    {
        try
        {
            var userProvider = provider.GetService<IUserProvider>();
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