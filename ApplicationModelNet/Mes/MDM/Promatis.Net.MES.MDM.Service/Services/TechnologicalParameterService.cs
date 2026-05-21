using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.MDM.Service;

/// <summary>
/// Финальный сервис для работы со справочником технологических параметров в MesMDM.
/// </summary>
public class TechnologicalParameterService(IDbContextFactory<MesMdmApplicationDbContext> contextFactory)
    : TechnologicalParameterService<TechnologicalParameter, MesMdmApplicationDbContext>(contextFactory)
{
    // Класс чист. Весь CRUD и специфичные методы унаследованы из абстрактного слоя.
}