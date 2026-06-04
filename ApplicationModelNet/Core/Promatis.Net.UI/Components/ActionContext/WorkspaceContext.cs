namespace Promatis.Net.UI.Components;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public class WorkspaceContext : IWorkspaceContext
{
    // Применяем коллекционные выражения C#
    protected readonly List<IUiControl> _controls = [];

    // Используем новый Lock для защиты коллекции от фоновых потоков (триггеров СУБД)
    private readonly Lock _lockObject = new();

    public IEnumerable<IUiControl> Controls
    {
        get
        {
            // Отдаем снапшот во избежание InvalidOperationException при рендеринге во время модификации
            lock (_lockObject)
            {
                return _controls.ToArray();
            }
        }
    }

    public event Action? OnContextStateChanged;
    public void NotifyStateChanged() => OnContextStateChanged?.Invoke();

    /// <summary>
    /// Базовая пустая реализация. Потомок может переопределить её 
    /// для автоматической сборки тулбаров или начальной настройки.
    /// </summary>
    public virtual void InitializeContext()
    {
        // По умолчанию ничего не делает, предоставляя холст для расширения
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Базовая пустая заглушка асинхронного жизненного цикла.
    /// Сложные контексты отчетов и журналов (например, AuditLogContext) переопределят 
    /// её для ленивого наполнения списков выбора фильтров после отрисовки каркаса.
    /// </summary>
    public virtual Task LoadMetadataAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Базовое состояние флага для статических страниц.
    /// Динамические контексты (DataContext) переопределят его своей true/false логикой.
    /// </summary>
    public virtual bool IsLoading => false;

    // --- ДЕФОЛТНАЯ НАСТРОЙКА ГЕОМЕТРИИ И СТИЛЕЙ ---
    public virtual int PaperElevation => 1;
    public virtual string PaperClass => "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";
    public virtual string WorkspaceHeight => "100%";
    public virtual string TopZoneHeight => "auto";
    public virtual string BottomZoneHeight => "auto";
    public virtual string LeftZoneWidth => "250px";
    public virtual string RightZoneWidth => "300px";

    // ИСПРАВЛЕНО: Свойства зон теперь принудительно пинают UI при изменении состояния сворачивания
    public bool IsTopZoneCollapsed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyStateChanged();
        }
    }

    public bool IsBottomZoneCollapsed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyStateChanged();
        }
    }

    public bool IsLeftZoneCollapsed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyStateChanged();
        }
    }

    public bool IsRightZoneCollapsed
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            NotifyStateChanged();
        }
    }

    // --- ПОТОКОБЕЗОПАСНЫЕ МЕТОДЫ УПРАВЛЕНИЯ ПАНЕЛЬЮ ДЛЯ ПОТОМКОВ ---
    protected void AddControl(IUiControl control)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));

        lock (_lockObject)
        {
            _controls.Add(control);
        }
        NotifyStateChanged();
    }

    protected void RemoveControl(string controlId)
    {
        lock (_lockObject)
        {
            _controls.RemoveAll(c => c.Id == controlId);
        }
        NotifyStateChanged();
    }
}