using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.Service;

public class UnitService<T>(IDbContextFactory<MdmApplicationDbContext> contextFactory)
    : UnitBaseService<T, MdmApplicationDbContext>(contextFactory)
    where T : UnitBase, ITreeNode<T>
{
}