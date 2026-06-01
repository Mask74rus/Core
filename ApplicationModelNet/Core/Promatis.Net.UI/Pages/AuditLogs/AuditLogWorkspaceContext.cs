using Promatis.Net.Domain;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Controls;
using Promatis.Net.UI.Pages.AuditLogs.Toolbar;

namespace Promatis.Net.UI.Pages.AuditLogs;

public class AuditLogWorkspaceContext : WorkspaceActionContext, IHasSelectedData<AuditLog>
{
    private AuditLog? _selectedData;

    // --- РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IHasSelectedData ---
    public new AuditLog? SelectedData
    {
        get => _selectedData;
        set
        {
            if (_selectedData != value)
            {
                _selectedData = value;
                OnContextUpdated?.Invoke();
                NotifyStateChanged();
            }
        }
    }

    public Action? OnContextUpdated { get; set; }

    // --- ГЕОМЕТРИЯ ПЯТИ ЗОН ---
    public override string TopZoneHeight => "48px";
    public override bool IsLeftZoneCollapsed => true;
    public override bool IsRightZoneCollapsed => true;
    public override bool IsBottomZoneCollapsed => true;

    public AuditLogWorkspaceContext(Action onFilterChanged)
    {
        // 1. Сначала добавляем элементы фильтрации
        AddControl(new AuditEntitySelect(onFilterChanged));
        AddControl(new AuditPeriodPicker(onFilterChanged));

        // 2. Ставим системный разделитель ядра
        AddControl(new ToolbarDivider());

        // 3. Добавляем универсальные generic CRUD кнопки для сущности AuditLog
        AddControl(new CreateEntityButton<AuditLog>());
        AddControl(new EditEntityButton<AuditLog>());
        AddControl(new DeleteEntityButton<AuditLog>());

        // 4. Еще один разделитель и кастомная кнопка выгрузки
        AddControl(new ToolbarDivider());
        AddControl(new AuditExportButton());
    }
}