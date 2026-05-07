using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public abstract class ReferenceService<T, TContext>(IDbContextFactory<TContext> contextFactory)
    : BaseService<T, Guid, TContext>(contextFactory), IReferenceService<T>
    where T : ReferenceBase
    where TContext : DbContext
{
    public async Task<T?> GetByCodeAsync(string code)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<List<T>> SearchByNameAsync(string namePart)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .Where(x => x.Name.Contains(namePart))
            .ToListAsync();
    }
}