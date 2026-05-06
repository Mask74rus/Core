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

        // Нам теперь нужен только Kind родителя
        UnitKind? parentKind = await args.Context.Set<UnitBase>()
            .Where(x => x.Id == unit.ParentId)
            .Select(x => (UnitKind?)x.Kind)
            .FirstOrDefaultAsync();

        if (parentKind.HasValue)
        {
            if (!IsHierarchyValid(parentKind.Value, unit.Kind))
            {
                args.Cancel = true;
                args.ErrorMessage = $"Нарушение иерархии: объект категории '{unit.Kind}' не может быть вложен в '{parentKind.Value}'.";
            }
        }
    }

    private static bool IsHierarchyValid(UnitKind parentKind, UnitKind childKind)
    {
        // 1. Правило для Department: могут быть все, кроме Position
        if (parentKind == UnitKind.Department)
        {
            return childKind != UnitKind.Position;
        }

        // 2. Правила для Production, Transport, Storage
        if (parentKind is UnitKind.Production or UnitKind.Transport or UnitKind.Storage)
        {
            // Не могут быть наследниками друг друга (разные Kind запрещены)
            // Могут содержать только себе подобных (вложенность) или Position
            return childKind == parentKind || childKind == UnitKind.Position;
        }

        // 3. Правило для Position: это конечная точка (терминальный узел)
        if (parentKind == UnitKind.Position)
        {
            return false;
        }

        return true;
    }
}