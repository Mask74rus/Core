using MudBlazor;

namespace Promatis.Net.UI.Components.Workspace;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public abstract class WorkspaceActionContext : IWorkspaceActionContext
{
    /// <summary>
    /// Провайдер служб текущей Scoped-сессии пользователя. 
    /// Заполняется автоматически визуальным холстом при старте страницы.
    /// </summary>
    public IServiceProvider ScopedProvider { get; set; } = null!;

    /// <summary>
    /// Уникальный идентификатор рабочей области в рантайме.
    /// </summary>
    public Guid WorkspaceId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Текстовый заголовок страницы (отображается на тулбаре или вкладке).
    /// </summary>
    public string PageTitle { get; set; } = "Рабочая область";

    /// <summary>
    /// Системное имя модуля (Core, Mes, MesMDM), к которому относится экран.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    // УПРАВЛЕНИЕ ВИЗУАЛЬНЫМ СТИЛЕМ ПОДЛОЖКИ
    public int PaperElevation { get; set; } = 1;
    public string PaperClass { get; set; } = "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";

    // УПРАВЛЕНИЕ ГЕОМЕТРИЕЙ КАРКАСА ХОЛСТА (Связь с WorkspacePage)
    public string WorkspaceHeight { get; set; } = "100%";
    public string TopZoneHeight { get; set; } = "auto";
    public string BottomZoneHeight { get; set; } = "auto";
    public string LeftZoneWidth { get; set; } = "250px";
    public string RightZoneWidth { get; set; } = "300px";

    public bool IsTopZoneCollapsed { get; set; } = false;
    public bool IsBottomZoneCollapsed { get; set; } = false;
    public bool IsLeftZoneCollapsed { get; set; } = false;
    public bool IsRightZoneCollapsed { get; set; } = false;

    // РЕАКТИВНЫЙ МОСТ ВЗАИМОДЕЙСТВИЯ (Blazor - Контекст)

    /// <summary>
    /// Событие, вызываемое при изменении свойств самого контекста (например, геометрии или заголовка).
    /// Принудительно заставляет Blazor перерисовать элементы.
    /// </summary>
    public Action? OnContextUpdated { get; set; }

    /// <summary>
    /// Триггер мгновенного оповещения UI-элементов о внутреннем изменении стейта контекста.
    /// </summary>
    public void NotifyUpdate() => OnContextUpdated?.Invoke();

    /// <summary>
    /// Глобальное событие, извещающее вложенные визуализаторы (грид, дерево) о необходимости обновить данные.
    /// Срабатывает при командах перезагрузки или по сигналам из СУБД.
    /// </summary>
    public event Action? OnRefreshRequested;

    /// <summary>
    /// Вспомогательный метод для безопасного запуска обновления вложенного контента изнутри контекста.
    /// </summary>
    protected void RequestRefresh() => OnRefreshRequested?.Invoke();

    /// <summary>
    /// Вызывается холстом WorkspacePage при успешном коммите любой сущности в СУБД.
    /// Переопределяется в типизированных наследниках для точечной проверки типов данных.
    /// </summary>
    /// <param name="state">Состояние изменения из перехватчика EF Core (EntityStateChangeEnum)</param>
    /// <param name="entity">Сам доменный объект (чистый или проксированный)</param>
    public virtual void HandleGlobalEntityCommit(object? state, object? entity)
    {
    }
}