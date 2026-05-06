namespace Promatis.Net.Domain;

public abstract class ReferenceTreeBase<T> : ReferenceBase where T : ReferenceTreeBase<T>
{
    public Guid? ParentId { get; set; }

    public virtual T? Parent { get; set; }

    public virtual ICollection<T> Children { get; set; } = new List<T>();
}