using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;

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

        if (parentInfo != null && !UnitHierarchyEngine.IsHierarchyValid(parentInfo.Kind, unit.Kind))
        {
            args.Cancel = true;
            args.ErrorMessage = $"Нарушение иерархии MES: Объект '{unit.Name}' ({unit.Kind}) " +
                                $"не может быть вложен в '{parentInfo.Name}' ({parentInfo.Kind}).";
        }
    }
}