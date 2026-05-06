namespace Promatis.Net.Service;

// Для справочников (добавляем поиск по коду/имени)
public interface IReferenceService<T> : IBaseService<T, Guid> where T : class
{
    Task<T?> GetByCodeAsync(string code);
    Task<List<T>> SearchByNameAsync(string namePart);
}