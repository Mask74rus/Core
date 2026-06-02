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
        {
            // Используем Array.Empty или пустую коллекцию без лишних аллокаций списков
            return new PagedResult<AuditLog>([], 0);
        }

        // ИСПРАВЛЕНО: Добавлена вторичная сортировка по Id для устранения эффекта "перемешивания" страниц СУБД
        List<AuditLog> items = await query
            .OrderByDescending(l => l.ChangedAt)
            .ThenByDescending(l => l.Id)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(items, totalCount);
    }

    public async Task<List<string>> GetAvailableEntityNamesAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedEntityNames != null)
        {
            return [.. _cachedEntityNames];
        }

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedEntityNames != null)
            {
                return [.. _cachedEntityNames];
            }

            await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync(cancellationToken);

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