namespace Promatis.Net.Data;

public class PropertyChangeInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public object? OriginalValue { get; set; }
    public object? CurrentValue { get; set; }
}