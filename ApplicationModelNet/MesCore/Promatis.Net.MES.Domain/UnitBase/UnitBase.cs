using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace Promatis.Net.MES.Domain;

/// <summary>
/// Абстрактная база для всех организационных и производственных единиц.
/// </summary>
public abstract class UnitBase : ReferenceTreeBase, ITreeNode<UnitBase>
{
    public UnitKind Kind { get; init; }
    public required UnitType Type { get; set; }

    // Эти свойства нужны только для удобства разработчика в коде C#, 
    // они просто приводят типы базового дерева и не участвуют в маппинге БД.
    [NotMapped] public UnitBase? TypedParent => Parent as UnitBase;
    [NotMapped] public IEnumerable<UnitBase> TypedChildren => Children.Cast<UnitBase>();
}