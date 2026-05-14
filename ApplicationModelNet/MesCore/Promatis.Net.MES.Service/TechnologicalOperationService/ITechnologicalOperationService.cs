using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalOperationService<T, TLink> : IReferenceTreeService<T>
    where T : ReferenceTreeBase, ITechnologicalOperation 
    where TLink : TechnologicalOperationUnitBase<T>      
{
    /// <summary>
    /// Получить список разрешенного оборудования (юнитов) для конкретной технологической операции.
    /// </summary>
    Task<List<UnitBase>> GetAllowedUnitsAsync(Guid operationId);
}