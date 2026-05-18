using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

/// <summary>
/// Сервис для работы с логами аудита.
/// Полностью совместим с базовым BaseService и автоматически регистрируется сканером.
/// </summary>
public class AuditLogService(IDbContextFactory<ApplicationDbContext> contextFactory)
    : BaseService<AuditLog, Guid, ApplicationDbContext>(contextFactory), IAuditLogService
{
    // Статический кэш в памяти для уникальных имен сущностей
    private static List<string>? _cachedEntityNames;
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    public async Task<PagedResult<AuditLog>> SearchLogsAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AuditLog> query = context.Set<AuditLog>()
            .AsNoTracking()
            .Where(l => l.ChangedAt >= request.FromDate && l.ChangedAt <= request.ToDate);

        if (!string.IsNullOrWhiteSpace(request.EntityName)) query = query.Where(l => l.EntityName == request.EntityName);

        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(l => l.Action == request.Action);

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
            return new PagedResult<AuditLog>([], 0);

        List<AuditLog> items = await query
            .OrderByDescending(l => l.ChangedAt)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(items, totalCount);
    }

    /// <summary>
    /// Получает список уникальных сущностей с использованием потокобезопасного кэширования.
    /// Исключает повторные тяжелые запросы Distinct к базе данных PostgreSQL.
    /// </summary>
    public async Task<List<string>> GetAvailableEntityNamesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Быстрая проверка: если кэш уже заполнен, мгновенно возвращаем копию данных
        if (_cachedEntityNames != null)
        {
            return [.. _cachedEntityNames]; // Используем синтаксис коллекций C# 12 для создания копии
        }

        // 2. Блокировка для предотвращения параллельных тяжелых запросов от разных пользователей
        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            // Повторная проверка внутри блокировки (Double-Check Locking паттерн)
            if (_cachedEntityNames != null)
            {
                return [.. _cachedEntityNames];
            }

            await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync(cancellationToken);

            // Выполняем тяжелый запрос только ОДИН раз за всё время жизни приложения
            _cachedEntityNames = await context.Set<AuditLog>()
                .AsNoTracking()
                .Select(l => l.EntityName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(cancellationToken);

            return [.. _cachedEntityNames];
        }
        finally
        {
            CacheLock.Release();
        }
    }

    /// <summary>
    /// Метод сброса кэша. 
    /// Вызывайте его, если динамически регистрируются новые модули в рантайме без перезапуска.
    /// </summary>
    public static void InvalidateCache()
    {
        CacheLock.Wait();
        try
        {
            _cachedEntityNames = null;
        }
        finally
        {
            CacheLock.Release();
        }
    }
}