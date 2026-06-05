namespace Promatis.Net.UI.Components;

/// <summary>
/// Корневой интерфейс пульта управления пространственной геометрией холста и стейтом отображения.
/// </summary>
public interface IWorkspaceContext
{
    /// <summary>
    /// Глобальный флаг индикации загрузки. При значении true обобщенный 
    /// каркас WorkspacePage блокирует экран и включает крутилку MudOverlay.
    /// </summary>
    public bool IsLoading { get; }

    // --- ПАРАМЕТРЫ СТИЛИЗАЦИИ И ГЕОМЕТРИИ 5 ЗОН ХОЛСТА СТРАНИЦЫ ---
    int PaperElevation { get; }
    string PaperClass { get; }
    string WorkspaceHeight { get; }
    string TopZoneHeight { get; }
    string BottomZoneHeight { get; }
    string LeftZoneWidth { get; }
    string RightZoneWidth { get; }

    // --- РЕАКТИВНЫЕ ФЛАГИ СХЛОПЫВАНИЯ ПАНЕЛЕЙ (Блейзор отследит мутации сам) ---
    bool IsTopZoneCollapsed { get; set; }
    bool IsBottomZoneCollapsed { get; set; }
    bool IsLeftZoneCollapsed { get; set; }
    bool IsRightZoneCollapsed { get; set; }
}