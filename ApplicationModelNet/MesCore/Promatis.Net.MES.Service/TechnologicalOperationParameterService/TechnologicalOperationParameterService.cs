using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class TechnologicalOperationParameterService<T, TOperation, TParameter, TContext>(
    IDbContextFactory<TContext> contextFactory)
    : BaseService<T, Guid, TContext>(contextFactory), ITechnologicalOperationParameterService<T, TOperation, TParameter>
    where T : TechnologicalOperationParameterBase<TOperation, TParameter>
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
    where TContext : DbContext
{
    public async Task<List<T>> GetByOperationIdAsync(Guid operationId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        return await context.Set<T>()
            .Where(x => x.OperationId == operationId)
            .Include(x => x.Parameter) // Сразу подтягиваем справочник параметров для UI
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<T>> GetRequiredParametersByOperationIdAsync(Guid operationId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        return await context.Set<T>()
            .Where(x => x.OperationId == operationId && x.IsRequired)
            .Include(x => x.Parameter)
            .AsNoTracking()
            .ToListAsync();
    }
}