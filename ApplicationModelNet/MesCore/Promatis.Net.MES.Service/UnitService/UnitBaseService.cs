using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class UnitBaseService<T, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceTreeService<T, TContext>(contextFactory), IUnitBaseService<T>
    where T : UnitBase
    where TContext : DbContext
{
    public async Task<List<T>> GetByKindAsync(UnitKind kind)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => x.Kind == kind)
            .ToListAsync();
    }

    public async Task<List<T>> GetByTypeAsync(UnitType type)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        // Используем побитовое "И", так как UnitType — это [Flags]
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => (x.Type & type) != 0)
            .ToListAsync();
    }

    public async Task<List<T>> GetByKindAndTypeAsync(UnitKind kind, UnitType type)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .AsNoTracking()
            .Where(x => x.Kind == kind && (x.Type & type) != 0)
            .ToListAsync();
    }
}