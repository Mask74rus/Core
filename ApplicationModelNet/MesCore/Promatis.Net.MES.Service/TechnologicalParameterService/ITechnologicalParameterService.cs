using Promatis.Net.MES.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface ITechnologicalParameterService<T> : IReferenceService<T>
    where T : TechnologicalParameterBase
{
    /// <summary>
    /// Получить параметры, отфильтрованные по типу данных (Numeric, String, Boolean, DateTime)
    /// </summary>
    Task<List<T>> GetByDataTypeAsync(string dataType);
}