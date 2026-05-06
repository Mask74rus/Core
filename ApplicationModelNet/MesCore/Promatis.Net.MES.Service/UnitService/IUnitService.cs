using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

public interface IUnitService : IReferenceTreeService<UnitBase>
{
    Task<List<UnitBase>> GetByKindAsync(UnitKind kind);
    Task<List<UnitBase>> GetByTypeAsync(UnitType type);
    Task<List<UnitBase>> GetByKindAndTypeAsync(UnitKind kind, UnitType type);
}