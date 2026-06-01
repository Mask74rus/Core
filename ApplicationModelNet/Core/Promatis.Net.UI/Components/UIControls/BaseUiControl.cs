namespace Promatis.Net.UI.Components;

/// <summary>
/// Базовая абстракция для всех элементов управления платформы (кнопок, чекбоксов, селектов, периодов).
/// Реализует инфраструктурную рутину и предоставляет мост к динамическому рендерингу (DynamicComponent).
/// </summary>
public abstract class BaseUiControl : IUiControl
{
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _isRunning;

    public abstract string Id { get; }
    public abstract Type ComponentType { get; }

    public virtual string? Title { get; init; }
    public virtual string? Icon { get; init; }
    public virtual string? Tooltip { get; init; }

    public Dictionary<string, object> ComponentParameters { get; } = new();

    public bool IsVisible
    {
        get => _isVisible;
        set { if (_isVisible != value) { _isVisible = value; NotifyStateChanged(); } }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (_isEnabled != value) { _isEnabled = value; NotifyStateChanged(); } }
    }

    public bool IsRunning => _isRunning;

    public event Action? OnStateChanged;

    protected BaseUiControl()
    {
        ComponentParameters.Add("Control", this);
    }

    protected void NotifyStateChanged() => OnStateChanged?.Invoke();

    public virtual bool IsEnabledForData(object? targetData) => true;

    public async Task TriggerAsync(object? targetData)
    {
        if (!_isEnabled || _isRunning || !IsEnabledForData(targetData)) return;

        try
        {
            _isRunning = true;
            NotifyStateChanged();
            await HandleTriggerAsync(targetData);
        }
        finally
        {
            _isRunning = false;
            NotifyStateChanged();
        }
    }

    protected abstract Task HandleTriggerAsync(object? targetData);
}