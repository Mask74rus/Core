using Promatis.Net.MES.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalOperationService<T, TLink> : IReferenceTreeService<T>
    where T : TechnologicalOperationBase
    where TLink : TechnologicalOperationUnitBase
{
    Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId);
}