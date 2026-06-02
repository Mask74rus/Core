namespace Promatis.Net.UI.Components;

/// <summary>
/// Единый контракт контекста рабочей области, управляющий геометрией пяти зон,
/// составом доступных UI-действий и текущим выделением данных на экране.
/// </summary>
public interface IWorkspaceActionContext
{
    // --- СЛОЙ УПРАВЛЕНИЯ КОМПОНЕНТАМИ И ДАННЫМИ ---

    /// <summary>
    /// Коллекция интерактивных элементов управления (кнопок, фильтров, переключателей) данного холста.
    /// </summary>
    IEnumerable<IUiControl> Controls { get; }

    event Action? OnContextStateChanged;
    void NotifyStateChanged();

    // --- ПАРАМЕТРЫ СТИЛИЗАЦИИ И ГЕОМЕТРИИ 5 ЗОН ---
    int PaperElevation { get; }
    string PaperClass { get; }
    string WorkspaceHeight { get; }
    string TopZoneHeight { get; }
    string BottomZoneHeight { get; }
    string LeftZoneWidth { get; }
    string RightZoneWidth { get; }

    bool IsTopZoneCollapsed { get; set; }
    bool IsBottomZoneCollapsed { get; set; }
    bool IsLeftZoneCollapsed { get; set; }
    bool IsRightZoneCollapsed { get; set; }
}