using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Service;

public abstract class BaseService<T, TKey>(IDbContextFactory<ApplicationDbContext> contextFactory)
    : IBaseService<T, TKey>
    where T : class, IDomainObjectHasKey<TKey>
{
    protected readonly IDbContextFactory<ApplicationDbContext> ContextFactory = contextFactory;

    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().FindAsync(id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TKey id)
    {
        await using ApplicationDbContext context = await ContextFactory.CreateDbContextAsync();
        T? entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}