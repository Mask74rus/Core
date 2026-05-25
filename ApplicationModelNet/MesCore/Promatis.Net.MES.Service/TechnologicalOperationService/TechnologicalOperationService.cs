using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public abstract class TechnologicalOperationService<T, TLink, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceTreeService<T, TContext>(contextFactory), ITechnologicalOperationService<T, TLink>
    where T : ReferenceTreeBase<T>, ITechnologicalOperation, IDomainObjectHasKey<Guid>, new()
    where TLink : TechnologicalOperationUnitBase<T>
    where TContext : DbContext
{
    public async Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId)
    {
        await using TContext context = await ContextFactory.CreateDbContextAsync();

        // LINQ-запрос отрабатывает идеально:
        // Благодаря тому, что в TechnologicalOperationUnitBase свойство Unit 
        // имеет строгий тип UnitBase, EF Core построит чистый SQL-JOIN автоматически.
        return await context.Set<TLink>()
            .Where(x => x.OperationId == operationId)
            .Select(x => x.Unit)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Оставляем метод абстрактным на текущем уровне. Конкретный прикладной сервис 
    /// техпроцессов сам переопределит его для создания своих полиморфных операций.
    /// </summary>
    public abstract override Task<T> CreateChildTemplateAsync(T parent);
}