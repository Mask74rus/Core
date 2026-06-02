using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Service;

public abstract class BaseService<T, TKey, TContext>(IDbContextFactory<TContext> contextFactory)
    : IBaseService<T, TKey>
    where T : class, IDomainObjectHasKey<TKey>
    where TContext : DbContext
{
    protected readonly IDbContextFactory<TContext> ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    public virtual async Task<T?> GetByIdAsync(TKey id)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().FindAsync(id);
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<T>().ToListAsync();
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // 1. Создаем базовый запрос к таблице сущности
        IQueryable<T> query = context.Set<T>();

        // 2. Получаем общее количество записей в БД для пагинатора
        int totalCount = await query.CountAsync(ct);

        // 3. Сортируем по первичному ключу (ID) для детерминированного порядка страниц.
        // Так как у нас есть ограничение where T : IDomainObjectHasKey<TKey>, мы можем безопасно использовать x.Id
        query = query.OrderBy(x => x.Id);

        // 4. Отрезаем нужную страницу с помощью Skip/Take и материализуем список
        List<T> items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // 5. Возвращаем упакованный результат
        return new PagedResult<T>(items, totalCount);
    }

    public virtual async Task AddAsync(T entity)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        await context.Set<T>().AddAsync(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        context.Set<T>().Update(entity);
        await context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TKey id)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();
        T? entity = await context.Set<T>().FindAsync(id);
        if (entity != null)
        {
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}