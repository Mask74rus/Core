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

    // --- УПРАВЛЕНИЕ ВИЗУАЛЬНЫМ СТИЛЕМ ПОДЛОЖКИ ---
    public int PaperElevation { get; set; } = 1;
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

    // =========================================================================
    // ДОБАВЛЕНО ДЛЯ РЕАКТИВНОГО UI (ВЕРХНИЙ ИНФРАСТРУКТУРНЫЙ ЭТАЖ)
    // =========================================================================

    /// <summary>
    /// Глобальное событие, извещающее вложенный UI-контент о необходимости обновить данные.
    /// </summary>
    public event Action? OnRefreshRequested;

    /// <summary>
    /// Вызывается верхним уровнем холста при коммите любой сущности.
    /// Переопределяется в типизированных наследниках для проверки соответствия типов.
    /// </summary>
    /// <param name="state">Состояние изменения из перехватчика EF Core (EntityStateChangeEnum)</param>
    /// <param name="entity"></param>
    public virtual void HandleGlobalEntityCommit(object state, object entity)
    {
    }

    /// <summary>
    /// Вспомогательный метод для безопасного запуска обновления вложенного контента изнутри контекста.
    /// </summary>
    protected void RequestRefresh()
    {
        OnRefreshRequested?.Invoke();
    }

    // =========================================================================
    // ГЛОБАЛЬНАЯ СИСТЕМА ЦВЕТОВОГО КОДИРОВАНИЯ ОПЕРАЦИЙ (ВЫНЕСЕНО ИЗ СТРАНИЦ)
    // =========================================================================

    /// <summary>
    /// Возвращает системный цвет MudBlazor на основе строкового или Enum действия.
    /// Централизованно обеспечивает цветовое единообразие во всех модулях MES/MDM.
    /// </summary>
    public virtual MudBlazor.Color GetActionColor(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return MudBlazor.Color.Default;

        return action.ToLower().Trim() switch
        {
            "create" or "insert" or "added" or "добавление" or "создание" => MudBlazor.Color.Success,
            "update" or "edit" or "modified" or "изменение" or "редактирование" => MudBlazor.Color.Warning,
            "delete" or "remove" or "deleted" or "softdeleted" or "удаление" => MudBlazor.Color.Error,
            _ => MudBlazor.Color.Default
        };
    }
}