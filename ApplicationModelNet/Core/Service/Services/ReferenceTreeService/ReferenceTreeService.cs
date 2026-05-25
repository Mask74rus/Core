using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Service;

/// <summary>
/// Универсальный базовый сервис для работы со всеми древовидными структурами в системе.
/// Наследует логику обычных справочников (ReferenceService) и расширяет её для иерархий.
/// </summary>
/// <typeparam name="T">Тип сущности, унаследованный от ReferenceTreeBase.</typeparam>
/// <typeparam name="TContext">Контекст базы данных.</typeparam>
public abstract class ReferenceTreeService<T, TContext>(IDbContextFactory<TContext> contextFactory)
    : ReferenceService<T, TContext>(contextFactory), IReferenceTreeService<T, TContext>
    where T : ReferenceTreeBase<T>, ITreeNode<T>, IDomainObjectHasKey<Guid>
    where TContext : DbContext
{
    private TreeServiceProxy? _treeServiceProxy;
    private TreeServiceProxy Engine => _treeServiceProxy ??= new TreeServiceProxy(ContextFactory, this);

    // =========================================================================
    // ЭЛЕГАНТНЫЙ ПРОБРОС ВЫЗОВОВ В ЕДИНЫЙ ДВИЖОК TREESERVICE (0% ДУБЛИРОВАНИЯ КОДА)
    // =========================================================================
    public Task<List<T>> GetRootsAsync() => Engine.GetRootsAsync();
    public Task<List<T>> GetChildrenAsync(Guid parentId) => Engine.GetChildrenAsync(parentId);
    public Task<List<T>> GetParentPathAsync(Guid id) => Engine.GetParentPathAsync(id);
    public Task<T?> GetFullTreeAsync(Guid rootId) => Engine.GetFullTreeAsync(rootId);
    public Task MoveAsync(Guid id, Guid? newParentId, CancellationToken ct = default) => Engine.MoveAsync(id, newParentId, ct);

    public abstract Task<T> CreateChildTemplateAsync(T parent);

    // ВНУТРЕННИЙ МОСТ-РЕАЛИЗАТОР ПЛАТФОРМЫ
    private class TreeServiceProxy(IDbContextFactory<TContext> factory, ReferenceTreeService<T, TContext> parentService)
        : TreeService<T, TContext>(factory)
    {
        public override Task<T> CreateChildTemplateAsync(T parent) => parentService.CreateChildTemplateAsync(parent);
    }
}
