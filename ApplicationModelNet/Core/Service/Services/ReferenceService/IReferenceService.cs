namespace Promatis.Net.Service;

// Для справочников
public interface IReferenceService<T> : IBaseService<T, Guid> where T : class
{
    Task<T?> GetByCodeAsync(string code);

    Task<List<T>> SearchByNameAsync(string namePart);
}