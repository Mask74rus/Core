using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class TechnologicalOperationService<T, TLink, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceTreeService<T, TContext>(contextFactory), ITechnologicalOperationService<T, TLink>
    where T : ReferenceTreeBase, ITechnologicalOperation
    where TLink : TechnologicalOperationUnitBase<T> 
    where TContext : DbContext
{
    public async Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // LINQ-запрос отрабатывает идеально:
        // Благодаря тому, что в TechnologicalOperationUnitBase свойство Unit 
        // имеет строгий тип UnitBase, EF Core построит чистый SQL-JOIN автоматически.
        return await context.Set<TLink>()
            .Where(x => x.OperationId == operationId)
            .Select(x => x.Unit)
            .AsNoTracking()
            .ToListAsync();
    }
}