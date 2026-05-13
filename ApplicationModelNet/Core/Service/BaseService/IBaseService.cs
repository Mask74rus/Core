namespace Promatis.Net.Service;

// Базовый CRUD
public interface IBaseService<T, in TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id);

    Task<List<T>> GetAllAsync();

    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    Task DeleteAsync(TKey id);
}