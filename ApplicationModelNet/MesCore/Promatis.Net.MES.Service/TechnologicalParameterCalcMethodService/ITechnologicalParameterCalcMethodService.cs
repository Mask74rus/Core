using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalParameterCalcMethodService<T, TUnit, TOperation, TParameter> : IBaseService<T, Guid>
    where T : TechnologicalParameterCalcMethodBase<TUnit, TOperation, TParameter>
    where TUnit : UnitBase
    where TOperation : DomainObject, ITechnologicalOperation
    where TParameter : TechnologicalParameterBase
{
    /// <summary>
    /// Найти конкретную инструкцию расчета для тройки: Оборудование + Операция + Параметр
    /// </summary>
    Task<T?> GetCalcMethodAsync(Guid unitId, Guid operationId, Guid parameterId);

    /// <summary>
    /// Получить все методы расчета для конкретной операции на оборудовании
    /// </summary>
    Task<List<T>> GetMethodsByOperationAndUnitAsync(Guid operationId, Guid unitId);
}