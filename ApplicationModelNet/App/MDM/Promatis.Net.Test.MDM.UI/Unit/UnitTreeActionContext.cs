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
    /// Создавать дочерние элементы (подузлы) можно для чего угодно, кроме конечных рабочих точек (ячеек/станков).
    /// </summary>
    protected override bool CanCreateChildNode(UnitBase node)
    {
        return node.Kind != UnitKind.Position;
    }

    /// <summary>
    /// Расширение логики перерасчета стейта. 
    /// Управляет реактивной видимостью кнопки «Добавить подузел» в зависимости от типа узла,
    /// предотвращая визуальное замусоривание тулбара.
    /// </summary>
    protected override void RecalculateButtonStates()
    {
        // 1. Даем базовой инфраструктуре проверить фокус на null
        base.RecalculateButtonStates();

        if (SelectedData == null)
        {
            IsCreateChildVisible = false;
            return;
        }

        // 2. Декларативно скрываем кнопку, если фокус находится на конечной позиции
        IsCreateChildVisible = SelectedData.Kind != UnitKind.Position;
    }
}