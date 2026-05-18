namespace Promatis.Net.Service;

public record AuditLogSearchRequest(
    DateTime FromDate,
    DateTime ToDate,
    string? EntityName = null,
    string? Action = null,
    int PageIndex = 0,
    int PageSize = 10);
