namespace Promatis.Net.UI.Components;

/// <summary>
/// Единый контракт контекста рабочей области, управляющий геометрией пяти зон,
/// составом доступных UI-действий и текущим выделением данных на экране.
/// </summary>
public interface IWorkspaceContext
{
    // --- СЛОЙ УПРАВЛЕНИЯ КОМПОНЕНТАМИ И ДАННЫМИ ---

    /// <summary>
    /// Коллекция интерактивных элементов управления данного холста.
    /// </summary>
    IEnumerable<IUiControl> Controls { get; }

    event Action? OnContextStateChanged;
    void NotifyStateChanged();

    /// <summary>
    /// Единая точка инициализации жизненного цикла любого контекста.
    /// Вызывается базовым холстом (WorkspacePage) при старте формы.
    /// </summary>
    void InitializeContext();

    /// <summary>
    /// ИСПРАВЛЕНО: Единая точка асинхронной загрузки метаданных фильтров и справочников.
    /// Вызывается автоматически базовым холстом СТРОГО ПОСЛЕ первичного рендеринга под защитой MudOverlay.
    /// </summary>
    Task LoadMetadataAsync(CancellationToken ct);

    /// <summary>
    /// ИСПРАВЛЕНО: Флаг индикации загрузки поднят на самый верх контракта,
    /// чтобы базовый холст страницы мог реактивно отображать крутилку MudOverlay.
    /// </summary>
    bool IsLoading { get; }

    // --- ПАРАМЕТРЫ СТИЛИЗАЦИИ И ГЕОМЕТРИИ 5 ЗОН ---
    int PaperElevation { get; }
    string PaperClass { get; }
    string WorkspaceHeight { get; }
    string TopZoneHeight { get; }
    string BottomZoneHeight { get; }
    string LeftZoneWidth { get; }
    string RightZoneWidth { get; }

    // ИСПРАВЛЕНО: Теперь интерфейс декларирует полноценное реактивное поведение
    bool IsTopZoneCollapsed { get; set; }
    bool IsBottomZoneCollapsed { get; set; }
    bool IsLeftZoneCollapsed { get; set; }
    bool IsRightZoneCollapsed { get; set; }
}