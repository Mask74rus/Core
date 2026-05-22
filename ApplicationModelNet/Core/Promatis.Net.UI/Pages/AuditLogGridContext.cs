using Promatis.Net.Domain;
using Promatis.Net.UI.Components.BaseGrid;

namespace Promatis.Net.UI.Pages;

/// <summary>
/// Специализированный контекст логов аудита, переопределяющий базовую фильтрацию типов
/// </summary>
public class AuditLogGridContext : GridActionContext<AuditLog>
{
    public override void HandleGlobalEntityCommit(object state, object entity)
    {
        RequestRefresh();
    }
}