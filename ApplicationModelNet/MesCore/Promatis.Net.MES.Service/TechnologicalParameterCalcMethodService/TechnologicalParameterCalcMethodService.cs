using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class TechnologicalParameterCalcMethodService<T, TUnit, TOperation, TParameter, TContext>(
    IDbContextFactory<TContext> contextFactory)
    : BaseService<T, Guid, TContext>(contextFactory), ITechnologicalParameterCalcMethodService<T, TUnit, TOperation, TParameter>
    where T : TechnologicalParameterCalcMethodBase<TUnit, TOperation, TParameter>
    where TUnit : UnitBase
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
    where TContext : DbContext
{
    public async Task<T?> GetCalcMethodAsync(Guid unitId, Guid operationId, Guid parameterId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        return await context.Set<T>()
            .FirstOrDefaultAsync(x => x.UnitId == unitId &&
                                      x.TechnologicalOperationId == operationId &&
                                      x.TechnologicalParameterId == parameterId);
    }

    public async Task<List<T>> GetMethodsByOperationAndUnitAsync(Guid operationId, Guid unitId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        return await context.Set<T>()
            .Where(x => x.TechnologicalOperationId == operationId && x.UnitId == unitId)
            .Include(x => x.TechnologicalParameter) // Подгружаем связанный параметр
            .AsNoTracking()
            .ToListAsync();
    }
}