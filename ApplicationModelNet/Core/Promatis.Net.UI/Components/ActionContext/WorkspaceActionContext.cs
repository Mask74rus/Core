namespace Promatis.Net.UI.Components;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public class WorkspaceActionContext : IWorkspaceActionContext
{
    protected readonly List<IUiControl> _controls = new();

    public IEnumerable<IUiControl> Controls => _controls;

    public event Action? OnContextStateChanged;

    public void NotifyStateChanged() => OnContextStateChanged?.Invoke();

    // --- ДЕФОЛТНАЯ НАСТРОЙКА ГЕОМЕТРИИ И СТИЛЕЙ ---
    public virtual int PaperElevation => 1;
    public virtual string PaperClass => "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";
    public virtual string WorkspaceHeight => "100%";
    public virtual string TopZoneHeight => "auto";
    public virtual string BottomZoneHeight => "auto";
    public virtual string LeftZoneWidth => "250px";
    public virtual string RightZoneWidth => "300px";

    public virtual bool IsTopZoneCollapsed => false;
    public virtual bool IsBottomZoneCollapsed => false;
    public virtual bool IsLeftZoneCollapsed => false;
    public virtual bool IsRightZoneCollapsed => false;

    // --- МЕТОДЫ УПРАВЛЕНИЯ ПАНЕЛЬЮ ДЛЯ ПОТОМКОВ ---
    protected void AddControl(IUiControl control)
    {
        _controls.Add(control);
        NotifyStateChanged();
    }

    protected void RemoveControl(string controlId)
    {
        _controls.RemoveAll(c => c.Id == controlId);
        NotifyStateChanged();
    }
}