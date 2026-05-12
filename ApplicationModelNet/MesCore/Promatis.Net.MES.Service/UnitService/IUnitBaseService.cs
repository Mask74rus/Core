using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface IUnitBaseService<T> : IReferenceTreeService<T>
    where T : UnitBase
{
    Task<List<T>> GetByKindAsync(UnitKind kind);
    Task<List<T>> GetByTypeAsync(UnitType type);
    Task<List<T>> GetByKindAndTypeAsync(UnitKind kind, UnitType type);
}