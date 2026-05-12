using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public interface IReferenceTreeService<T> : IReferenceService<T> where T : ReferenceTreeBase
{
    Task<List<T>> GetRootsAsync();

    Task<List<T>> GetChildrenAsync(Guid parentId);

    // Получение всей ветки (хлебные крошки)
    Task<List<T>> GetParentPathAsync(Guid id);

    Task<T?> GetFullTreeAsync(Guid rootId);

    /// <summary>
    /// Перемещает узел к новому родителю с проверкой иерархии и циклов.
    /// </summary>
    /// <param name="id">ID перемещаемого узла.</param>
    /// <param name="newParentId">ID нового родителя (null для корня).</param>
    Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default);
}