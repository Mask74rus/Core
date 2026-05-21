using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalOperationParameterService<T, TOperation, TParameter> : IBaseService<T, Guid>
    where T : TechnologicalOperationParameterBase<TOperation, TParameter>
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
{
    /// <summary>
    /// Получить все параметры, привязанные к конкретной операции
    /// </summary>
    Task<List<T>> GetByOperationIdAsync(Guid operationId);

    /// <summary>
    /// Получить только обязательные параметры для операции
    /// </summary>
    Task<List<T>> GetRequiredParametersByOperationIdAsync(Guid operationId);
}