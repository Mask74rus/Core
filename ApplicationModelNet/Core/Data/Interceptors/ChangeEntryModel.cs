namespace Promatis.Net.Data;

internal class ChangeEntryModel
{
    public object Entity { get; init; } = null!;
    public EntityStateChangeEnum State { get; init; }
    public List<PropertyChangeInfo> Changes { get; init; } = [];
    public string? ChangedBy { get; init; }
    public DateTime ChangedAt { get; init; }
}