namespace Promatis.Net.Domain;

public abstract class ReferenceTreeBase : ReferenceBase
{
    public Guid? ParentId { get; set; }

    // EF Core идеально понимает эту связь по умолчанию!
    public virtual ReferenceTreeBase? Parent { get; set; }
    public virtual ICollection<ReferenceTreeBase> Children { get; set; } = new List<ReferenceTreeBase>();
}