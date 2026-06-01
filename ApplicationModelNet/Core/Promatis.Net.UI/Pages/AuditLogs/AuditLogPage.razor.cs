using Microsoft.AspNetCore.Components;
using Promatis.Net.Domain;

namespace Promatis.Net.UI.Pages.AuditLogs;

public partial class AuditLogPage : ComponentBase
{
    protected AuditLogWorkspaceContext Context { get; set; } = null!;
    protected List<AuditLog> Logs { get; set; } = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Инициализируем локальный контекст формы (DI-сканер его теперь пропускает)
        Context = new AuditLogWorkspaceContext(onFilterChanged: RefreshLogsData);

        RefreshLogsData();
    }

    protected void RefreshLogsData()
    {
        // Временные mock-данные (заглушка)
        Logs = new List<AuditLog>
        {
            new() {
                EntityName = "User",
                EntityId = Guid.NewGuid(),
                Action = "Create",
                ChangedAt = DateTime.Now.AddMinutes(-30),
                ChangedBy = "system_admin",
                ChangesJson = "{\"Login\": \"a.petrov\"}"
            },
            new() {
                EntityName = "Equipment",
                EntityId = Guid.NewGuid(),
                Action = "Delete",
                ChangedAt = DateTime.Now.AddMinutes(-5),
                ChangedBy = "operator_3",
                ChangesJson = "{\"Id\": \"d84f...\"}"
            }
        };

        InvokeAsync(StateHasChanged);
    }
}