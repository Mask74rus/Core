namespace Promatis.Net.Data;

public abstract class EntityEventArgsBase(
    object entity,
    EntityStateChangeEnum state,
    List<PropertyChangeInfo> changes) : EventArgs
{
    public object Entity { get; } = entity;
    public EntityStateChangeEnum State { get; } = state;
    public List<PropertyChangeInfo> Changes { get; } = changes;
    public bool Handled { get; set; }
}