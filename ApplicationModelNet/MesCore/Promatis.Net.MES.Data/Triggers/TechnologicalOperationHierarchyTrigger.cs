using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Триггер контроля иерархии (остается без изменений)
/// </summary>
public class TechnologicalOperationHierarchyTrigger : IBeforeSaveTrigger<TechnologicalOperationBase>
{
    public async Task HandleAsync(EntityCancelEventArgs<TechnologicalOperationBase> args)
    {
        var op = args.Entity;
        if (op.ParentId.HasValue && op.ParentId != Guid.Empty)
        {
            var parent = await args.Context.Set<TechnologicalOperationBase>()
                .Select(x => new { x.Id, x.IsLeaf })
                .FirstOrDefaultAsync(x => x.Id == op.ParentId);

            if (parent?.IsLeaf == true)
            {
                args.Cancel = true;
                args.ErrorMessage = "Нельзя добавить дочерний элемент в конечную операцию (Leaf).";
            }
        }
    }
}