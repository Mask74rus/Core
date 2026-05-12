using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class TechnologicalOperationService<T, TLink, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceTreeService<T, TContext>(contextFactory), ITechnologicalOperationService<T, TLink>
    where T : TechnologicalOperationBase
    where TLink : TechnologicalOperationUnitBase
    where TContext : DbContext
{
    public async Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        return await context.Set<TLink>()
            .Where(x => x.OperationId == operationId)
            .Select(x => x.Unit)
            .AsNoTracking()
            .ToListAsync();
    }
}