using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Data;
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
    public override async Task<List<TechnologicalParameter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using MesApplicationDbContext context = await ContextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<TechnologicalParameter>()
            .Include(x => x.UnitOfMeasurement) // Жадная загрузка справочника Ед. Изм.
            .ToListAsync(cancellationToken);
    }
}