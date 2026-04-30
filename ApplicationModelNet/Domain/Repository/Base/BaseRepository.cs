namespace Promatis.Net.Domain;

public class BaseRepository<T, TKey>(IDbContextFactory<ApplicationDbContext> contextFactory) : IBaseRepository<T, TKey>
    where T : class, IDomainObjectHasKey<TKey>
{
    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.Set<T>().FindAsync(id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TKey id)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        T? entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
