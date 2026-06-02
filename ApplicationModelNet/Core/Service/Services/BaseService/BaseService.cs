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
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // 1. Извлекаем АКТУАЛЬНЫЙ оригинальный объект из базы данных по его первичному ключу.
        // Благодаря constraints у нас гарантированно есть доступ к x.Id
        T? dbEntity = await context.Set<T>().FindAsync(entity.Id);

        if (dbEntity == null)
        {
            throw new InvalidOperationException($"Не удалось обновить объект: сущность '{typeof(T).Name}' с ID '{entity.Id}' не найдена в базе данных.");
        }

        // 2. Считываем метаданные отслеживания для оригинального объекта СУБД
        var entry = context.Entry(dbEntity);

        // 3. Переносим значения ТОЛЬКО ПРИМИТИВНЫХ свойств из клона в оригинальный объект.
        // Метод SetValues автоматически проигнорирует навигационные свойства (Parent, Children)
        // и коллекции отношений, которые ваш клонер вырезал! Он обновит только плоские поля.
        entry.CurrentValues.SetValues(entity);

        // 4. Дополнительная защита: проверяем, изменилось ли хоть что-то.
        // Если пользователь открыл диалог, ничего не менял и нажал Сохранить,
        // мы вообще не будем дёргать транзакцию PostgreSQL!
        if (entry.State == EntityState.Unchanged)
        {
            return;
        }

        // 5. EF Core сгенерирует хирургический SQL-запрос, обновив в БД ТОЛЬКО измененные колонки.
        // Существующие связи и изменения других пользователей в этот момент не пострадают!
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