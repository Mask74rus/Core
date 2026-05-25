using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.UI.Components.BaseTree;

namespace Promatis.Net.Test.MDM.UI.Unit;

/// <summary>
/// Специфичный доменный контекст управления тулбаром для дерева структуры предприятия и оборудования.
/// Связывает универсальную инфраструктуру дерева с бизнес-логикой объектов UnitBase.
/// </summary>
public class UnitTreeActionContext : TreeActionContext<UnitBase>
{
    /// <summary>
    /// Декларативное бизнес-правило завода:
    /// Создавать дочерние элементы (подузлы) можно для любого объекта структуры, 
    /// кроме конечных рабочих позиций (станков, рабочих мест, ячеек).
    /// </summary>
    protected override bool CanCreateChildNode(UnitBase node)
    {
        // Кнопка "Добавить подузел" станет серой (Disabled), если выбрана конечная позиция
        return node.Kind != UnitKind.Position;
    }
}