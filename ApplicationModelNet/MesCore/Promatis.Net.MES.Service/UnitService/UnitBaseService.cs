using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class UnitBaseService<TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceTreeService<UnitBase, TContext>(contextFactory), IUnitBaseService
    where TContext : DbContext
{
    public async Task<List<UnitBase>> GetByKindAsync(UnitKind kind)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<UnitBase>()
            .AsNoTracking()
            .Where(x => x.Kind == kind)
            .ToListAsync();
    }

    public async Task<List<UnitBase>> GetByTypeAsync(UnitType type)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        // Используем побитовое "И", так как UnitType — это [Flags]
        return await context.Set<UnitBase>()
            .AsNoTracking()
            .Where(x => (x.Type & type) != 0)
            .ToListAsync();
    }

    public async Task<List<UnitBase>> GetByKindAndTypeAsync(UnitKind kind, UnitType type)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<UnitBase>()
            .AsNoTracking()
            .Where(x => x.Kind == kind && (x.Type & type) != 0)
            .ToListAsync();
    }
}