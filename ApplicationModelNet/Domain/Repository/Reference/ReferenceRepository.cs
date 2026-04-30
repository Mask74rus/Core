namespace Promatis.Net.Domain;

public class ReferenceRepository<T>(IDbContextFactory<ApplicationDbContext> contextFactory)
    : BaseRepository<T, Guid>(contextFactory), IReferenceRepository<T>
    where T : ReferenceBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<T?> GetByCodeAsync(string code)
    {
        await using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>().FirstOrDefaultAsync(x => x.Code == code);
    }

    public async Task<List<T>> SearchByNameAsync(string namePart)
    {
        await using ApplicationDbContext context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<T>()
            .Where(x => x.Name.Contains(namePart))
            .ToListAsync();
    }
}