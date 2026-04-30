namespace Promatis.Net.Domain;

public interface IBaseRepository<T, TKey> where T : class, IDomainObjectHasKey<TKey>
{
    Task<T?> GetByIdAsync(TKey id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(TKey id);
}