using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для всех организационных и производственных единиц.
/// </summary>
public abstract class UnitBase : ReferenceTreeBase, ITreeNode<UnitBase>
{
    public UnitKind Kind { get; init; }
    public required UnitType Type { get; set; }
    public virtual UnitBase? Parent { get; set; }
    public virtual ICollection<UnitBase> Children { get; set; } = new List<UnitBase>();
}