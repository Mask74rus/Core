using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.Domain;
using Promatis.Net.Service;

namespace Promatis.Net.MES.Service;

/// <summary>
/// Сервис управления нормативно-справочной информацией единиц измерения.
/// </summary>
public class UnitOfMeasurementService(IDbContextFactory<MesApplicationDbContext> contextFactory)
    : ReferenceService<UnitOfMeasurement, MesApplicationDbContext>(contextFactory), IUnitOfMeasurementService
{
}