using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.Service;

/// <summary>
/// Конкретная реализация сервиса технологических операций для MDM.
/// </summary>
public class TechnologicalOperationService(IDbContextFactory<MdmApplicationDbContext> contextFactory)
    : TechnologicalOperationService<TechnologicalOperation, TechnologicalOperationUnit, MdmApplicationDbContext>(contextFactory)
{

}