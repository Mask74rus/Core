using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.MDM.Service;

/// <summary>
/// Финальный сервис для управления инструкциями и методами расчета телеметрии.
/// </summary>
public class TechnologicalParameterCalcMethodService(IDbContextFactory<MesMdmApplicationDbContext> contextFactory)
    : TechnologicalParameterCalcMethodService<
        TechnologicalParameterCalcMethod,
        UnitBase,
        TechnologicalOperation,
        TechnologicalParameter,
        MesMdmApplicationDbContext>(contextFactory)
{
    // Полный спектр бизнес-логики для настройки агрегации данных готов.
}