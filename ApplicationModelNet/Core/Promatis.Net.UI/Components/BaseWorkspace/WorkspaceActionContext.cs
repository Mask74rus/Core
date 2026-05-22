namespace Promatis.Net.UI.Components.BaseWorkspace;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public abstract class WorkspaceActionContext
{
    public Guid WorkspaceId { get; init; } = Guid.NewGuid();
    public string PageTitle { get; set; } = "Рабочая область";
    public string ModuleName { get; init; } = string.Empty;

    // --- УПРАВЛЕНИЕ ВИЗУАЛЬНЫМ СТИЛЕМ ПОДЛОЖКИ (Вместо PlainView флага) ---

    /// <summary>
    /// Интенсивность тени подложки MudPaper (по умолчанию 1 для справочников, 0 для мнемосхем)
    /// </summary>
    public int PaperElevation { get; set; } = 1;

    /// <summary>
    /// Дополнительные CSS-классы для подложки (по умолчанию pa-4 для отступов справочников, pa-0 для мнемосхем)
    /// </summary>
    public string PaperClass { get; set; } = "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";

    // --- УПРАВЛЕНИЕ ГЕОМЕТРИЕЙ КАРКАСА ---
    public string WorkspaceHeight { get; set; } = "100%";
    public string TopZoneHeight { get; set; } = "auto";
    public string BottomZoneHeight { get; set; } = "auto";
    public string LeftZoneWidth { get; set; } = "250px";
    public string RightZoneWidth { get; set; } = "300px";

    public bool IsTopZoneCollapsed { get; set; } = false;
    public bool IsBottomZoneCollapsed { get; set; } = false;
    public bool IsLeftZoneCollapsed { get; set; } = false;
    public bool IsRightZoneCollapsed { get; set; } = false;

    public Action? OnContextUpdated { get; set; }
    public void NotifyUpdate() => OnContextUpdated?.Invoke();
}