using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public interface IAuditLogService
{
    /// <summary>
    /// Поиск логов аудита по диапазону дат с поддержкой фильтрации и пагинации.
    /// </summary>
    Task<PagedResult<AuditLog>> SearchLogsAsync(AuditLogSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получение списка уникальных имен сущностей, которые есть в логах (для выпадающего списка в фильтре MudBlazor).
    /// </summary>
    Task<List<string>> GetAvailableEntityNamesAsync(CancellationToken cancellationToken = default);
}