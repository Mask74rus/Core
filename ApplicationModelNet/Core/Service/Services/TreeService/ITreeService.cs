using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Service;

/// <summary>
/// Глобальный платформенный контракт для абсолютно любого сервиса, управляющего деревьями.
/// Наследует базовый CRUD платформы и расширяет его иерархическими методами управления графом.
/// </summary>
/// <typeparam name="T">Тип доменной сущности, реализующей контракт ITreeNode.</typeparam>
public interface ITreeService<T> : IBaseService<T, Guid>
    where T : class, ITreeNode<T>, IDomainObjectHasKey<Guid>
{
    Task<List<T>> GetRootsAsync();
    Task<List<T>> GetChildrenAsync(Guid parentId);
    Task<List<T>> GetParentPathAsync(Guid id);
    Task<T?> GetFullTreeAsync(Guid rootId);
    Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default);
    Task<T> CreateChildTemplateAsync(T parent);
}