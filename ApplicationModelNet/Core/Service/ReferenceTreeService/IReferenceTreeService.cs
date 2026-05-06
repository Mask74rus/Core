using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public interface IReferenceTreeService<T> : IReferenceService<T> where T : ReferenceTreeBase<T>
{
    Task<List<T>> GetRootsAsync();
    Task<List<T>> GetChildrenAsync(Guid parentId);
    // Получение всей ветки (хлебные крошки)
    Task<List<T>> GetParentPathAsync(Guid id);

    Task<T?> GetFullTreeAsync(Guid rootId);
}