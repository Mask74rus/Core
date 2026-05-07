using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Триггер для проверки специфичных правил иерархии UnitBase.
/// </summary>
public class UnitBaseHierarchyTrigger : IBeforeSaveTrigger<UnitBase>
{
    public async Task HandleAsync(EntityCancelEventArgs<UnitBase> args)
    {
        UnitBase unit = args.Entity;

        if (!unit.ParentId.HasValue || unit.ParentId == Guid.Empty) return;

        // Получаем Kind и Name родителя для информативного сообщения
        var parentInfo = await args.Context.Set<UnitBase>()
            .Where(x => x.Id == unit.ParentId)
            .Select(x => new { x.Kind, x.Name })
            .FirstOrDefaultAsync();

        if (parentInfo != null)
        {
            if (!IsHierarchyValid(parentInfo.Kind, unit.Kind))
            {
                args.Cancel = true;
                args.ErrorMessage = $"Нарушение иерархии MES: Объект '{unit.Name}' ({unit.Kind}) " +
                                    $"не может быть вложен в '{parentInfo.Name}' ({parentInfo.Kind}).";
            }
        }
    }

    private static bool IsHierarchyValid(UnitKind parentKind, UnitKind childKind)
    {
        // 1. Правило для Position: это терминальный узел (маска Position не может быть родителем)
        // Используем HasFlag или побитовое И, так как Kind теперь - это набор флагов
        if (parentKind == UnitKind.Position)
        {
            return false;
        }

        // 2. Правило для Department: может содержать всё, кроме Position
        if (parentKind == UnitKind.Department)
        {
            return childKind != UnitKind.Position;
        }

        // 3. Правила для специализированных зон (Production, Transport, Storage)
        // Они не могут содержать друг друга, но могут содержать свои подтипы или Position
        if (parentKind is UnitKind.Production or UnitKind.Transport or UnitKind.Storage)
        {
            // Разрешаем, если ребенок того же вида или является Position
            return childKind == parentKind || childKind == UnitKind.Position;
        }

        return true;
    }
}