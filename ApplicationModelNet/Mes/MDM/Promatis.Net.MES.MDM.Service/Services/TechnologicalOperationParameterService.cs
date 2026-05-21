using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.MDM.Service;

/// <summary>
/// Финальный сервис для управления привязками параметров к операциям.
/// </summary>
public class TechnologicalOperationParameterService(IDbContextFactory<MesMdmApplicationDbContext> contextFactory)
    : TechnologicalOperationParameterService<
        TechnologicalOperationParameter,
        TechnologicalOperation,
        TechnologicalParameter,
        MesMdmApplicationDbContext>(contextFactory)
{
    // Класс готов к использованию в бизнес-логике и UI.
}