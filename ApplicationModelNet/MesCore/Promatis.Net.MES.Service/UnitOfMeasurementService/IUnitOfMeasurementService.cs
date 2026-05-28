using Promatis.Net.MES.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

/// <summary>
/// Контракт сервисного слоя для управления справочником единиц измерения.
/// </summary>
public interface IUnitOfMeasurementService : IReferenceService<UnitOfMeasurement>
{
}