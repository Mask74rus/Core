using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public abstract class ReferenceService<T>(IDbContextFactory<ApplicationDbContext> contextFactory)
    : BaseService<T, Guid>(contextFactory)
    where T : ReferenceBase
{
    public async Task<T?> GetByCodeAsync(string code)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<List<T>> SearchByNameAsync(string namePart)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .Where(x => x.Name.Contains(namePart))
            .ToListAsync();
    }
}