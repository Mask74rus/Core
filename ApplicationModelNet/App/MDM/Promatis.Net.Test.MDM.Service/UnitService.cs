using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.Service;

public class UnitService(IDbContextFactory<MdmApplicationDbContext> contextFactory)
    : UnitBaseService<UnitBase, MdmApplicationDbContext>(contextFactory)
{

}