using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalOperationService<T, TLink> : ITreeService<T>
    where T : ReferenceTreeBase<T>, ITechnologicalOperation, Promatis.Net.Domain.Interface.IDomainObjectHasKey<Guid>
    where TLink : class
{
    /// <summary>
    /// Получить список разрешенного оборудования (юнитов) для конкретной технологической операции.
    /// </summary>
    Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId);
}