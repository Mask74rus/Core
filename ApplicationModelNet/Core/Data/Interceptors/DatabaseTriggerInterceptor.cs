using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

/// <summary>
/// Перехватчик операций DbContext, обеспечивающий работу "мягкого удаления" (Soft Delete),
/// предварительную валидацию изменений и сквозное триггерное уведомление системы до/после сохранения данных.
/// </summary>
/// <param name="scopeFactory">Фабрика для создания изолированных областей видимости (Scope) служб.</param>
public class DatabaseTriggerInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    /// <summary>
    /// Потокобезопасное хранилище зафиксированных изменений в рамках текущей транзакции/запроса.
    /// Ключ — уникальный идентификатор экземпляра DbContext.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, List<ChangeEntryModel>> _capturedChanges = new();

    /// <summary>
    /// Асинхронный перехватчик, выполняемый ДО фактического сохранения изменений в базу данных.
    /// Отвечает за логику Soft Delete, фиксацию снимка изменений и запуск триггеров валидации.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context == null) return result;

        // Создаем локальный гарантированно живой Scope для предотвращения проблем со временем жизни DbContext / Scoped сервисов
        using IServiceScope scope = scopeFactory.CreateScope();
        var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();

        string? userName = await GetUserNameInternalAsync(scope.ServiceProvider);

        // 1. Предварительная обработка Soft Delete (Мягкое удаление)
        // Находим все сущности с интерфейсом ISoftDeletable, у которых статус "Удален"
        IEnumerable<EntityEntry<ISoftDeletable>> entries = eventData.Context.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted);

        foreach (EntityEntry<ISoftDeletable> entry in entries)
        {
            // Подменяем физическое удаление на обновление данных в БД
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = DateTime.UtcNow;
            entry.Entity.DeletedBy = userName;
        }

        // Принудительно заставляем EF Core пересчитать дерево изменений после подмены статусов
        eventData.Context.ChangeTracker.DetectChanges();

        // 2. Захват изменений (сохраняет нативные типы данных object? для тестов)
        List<ChangeEntryModel> captured = CaptureChanges(eventData.Context, userName);

        // Привязываем коллекцию изменений к конкретному экземпляру контекста базы данных
        _capturedChanges[eventData.Context.ContextId.InstanceId] = captured;

        // 3. Валидация (BeforeSave) через живой triggerService
        // Запускает доменные правила и валидаторы перед тем, как данные попадут в БД. При ошибке — транзакция прервется.
        foreach (ChangeEntryModel item in captured)
            await triggerService.ValidateAsync(item.Entity, item.State, item.Changes, eventData.Context);

        return result;
    }

    /// <summary>
    /// Асинхронный перехватчик, выполняемый ПОСЛЕ успешного сохранения изменений в базу данных.
    /// Отвечает за уведомление подписчиков и триггеров пост-обработки (фаза Saved).
    /// </summary>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        // Извлекаем захваченные изменения и сразу атомарно удаляем их из словаря, освобождая память
        if (eventData.Context != null && _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out List<ChangeEntryModel>? entries))
        {
            // Создаем локальный гарантированно живой Scope для фазы Saved (выполняется параллельно/после завершения транзакции)
            using IServiceScope scope = scopeFactory.CreateScope();
            var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();

            // Асинхронно уведомляем систему об изменениях (например, для обновления UI через MudBlazor или отправки шины событий)
            foreach (ChangeEntryModel item in entries)
                await triggerService.NotifyAsync(item.Entity, item.State, item.Changes, item.ChangedBy, item.ChangedAt);
        }
        return result;
    }

    /// <summary>
    /// Перехватчик сбоя при сохранении изменений. Гарантирует очистку кэша зафиксированных изменений,
    /// предотвращая утечки памяти при падении транзакций.
    /// </summary>
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken ct = default)
    {
        if (eventData.Context != null)
            _capturedChanges.TryRemove(eventData.Context.ContextId.InstanceId, out _);

        return base.SaveChangesFailedAsync(eventData, ct);
    }

    /// <summary>
    /// Внутренний метод анализа трекера изменений EF Core. 
    /// Вычисляет дельту (старые/новые значения полей) и формирует модели изменений.
    /// </summary>
    /// <param name="context">Текущий контекст базы данных.</param>
    /// <param name="userName">Имя пользователя, совершившего операцию.</param>
    /// <returns>Список структурированных моделей изменений <see cref="ChangeEntryModel"/>.</returns>
    private List<ChangeEntryModel> CaptureChanges(DbContext context, string? userName)
    {
        // Фильтруем только сущности в состояниях Добавления, Изменения или Удаления, игнорируя системные логи аудита
        List<EntityEntry> entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !e.Entity.GetType().Name.Contains("AuditLog"))
            .ToList();

        var result = new List<ChangeEntryModel>();

        foreach (EntityEntry e in entries)
        {
            EntityStateChangeEnum state = MapState(e.State);

            // Специфичный маппинг состояния для "мягко удаленных" записей
            if (e.Entity is ISoftDeletable soft)
            {
                PropertyEntry prop = e.Property(nameof(ISoftDeletable.DeletedAt));
                if (prop.IsModified && soft.DeletedAt != null) state = EntityStateChangeEnum.SoftDeleted;
            }

            List<PropertyChangeInfo> changes = new();

            // Сценарий 1: Сущность добавлена. Все текущие значения свойств считаются изменениями.
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
            // Сценарий 2: Сущность изменена или мягко удалена. Вычисляем разницу между старым и новым значениями.
            else if (state is EntityStateChangeEnum.Modified or EntityStateChangeEnum.SoftDeleted)
            {
                PropertyValues originalValues = e.OriginalValues;

                foreach (PropertyEntry p in e.Properties.Where(p => p.IsModified))
                {
                    string propertyName = p.Metadata.Name;
                    object? originalValue = originalValues[propertyName];
                    object? currentValue = p.CurrentValue;

                    // Фиксируем изменение только если значения действительно различаются
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

            // Даже если коллекция точечных изменений полей (Changes) пуста 
            // из-за рантайм-биндинга Blazor, мы ВСЁ РАВНО добавляем сущность в список уведомлений, 
            // если фабрика зафиксировала состояние Modified! Это гарантирует 100% доставку событий изменений в UI.
            if (state == EntityStateChangeEnum.Modified && changes.Count == 0)
            {
                // Создаем фиктивную запись изменения для доменных триггеров, 
                // чтобы не ломать валидацию, но сохранить импульс для UI
                changes.Add(new PropertyChangeInfo
                {
                    PropertyName = "Id",
                    OriginalValue = e.Property("Id").OriginalValue,
                    CurrentValue = e.Property("Id").CurrentValue
                });
            }

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

    /// <summary>
    /// Маппинг внутренних состояний EntityFramework State во внутренний доменный перечислитель системы.
    /// </summary>
    private EntityStateChangeEnum MapState(EntityState state) => state switch
    {
        EntityState.Added => EntityStateChangeEnum.Added,
        EntityState.Deleted => EntityStateChangeEnum.Deleted,
        _ => EntityStateChangeEnum.Modified
    };

    /// <summary>
    /// Безопасное получение имени текущего пользователя из контекста авторизации системы.
    /// </summary>
    /// <param name="provider">Локальный провайдер служб (DI Container).</param>
    private async Task<string?> GetUserNameInternalAsync(IServiceProvider provider)
    {
        try
        {
            var userProvider = provider.GetService<IUserProvider>();
            if (userProvider != null)
                return await userProvider.GetCurrentUserNameAsync();
        }
        catch
        {
            // ignored
        }

        return "System";
    }
}