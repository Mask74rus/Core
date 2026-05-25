using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;


namespace Promatis.Net.Service;

public interface IReferenceTreeService<T, TContext> : IReferenceService<T>, ITreeService<T>
    where T : ReferenceTreeBase<T>, ITreeNode<T>, IDomainObjectHasKey<Guid>
    where TContext : DbContext
{
    // С сквозным наследованием интерфейсов всё сошлось!
}