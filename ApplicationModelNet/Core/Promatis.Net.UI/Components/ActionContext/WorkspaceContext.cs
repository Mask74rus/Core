namespace Promatis.Net.UI.Components;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public class WorkspaceContext : IWorkspaceContext
{
    /// <summary>
    /// По умолчанию для статических экранов загрузка всегда выключена.
    /// Тяжелые дата-контексты (DataContext) переопределят это свойство своей true/false логикой.
    /// </summary>
    public virtual bool IsLoading => false;

    // --- ДЕФОЛТНЫЕ СТИЛИ И ГЕОМЕТРИЧЕСКИЕ РАЗМЕРЫ ПЛАТФОРМЫ ---
    public virtual int PaperElevation => 1;
    public virtual string PaperClass => "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";
    public virtual string WorkspaceHeight => "100%";
    public virtual string TopZoneHeight => "auto";
    public virtual string BottomZoneHeight => "auto";
    public virtual string LeftZoneWidth => "250px";
    public virtual string RightZoneWidth => "300px";

    // --- АВТОСВОЙСТВА НА СИНТАКСИСЕ field (ЧИСТЫЙ STATE CONTAINER ДЛЯ БЛЕЙЗОРА) ---
    public bool IsTopZoneCollapsed { get; set; }
    public bool IsBottomZoneCollapsed { get; set; }
    public bool IsLeftZoneCollapsed { get; set; }
    public bool IsRightZoneCollapsed { get; set; }
}