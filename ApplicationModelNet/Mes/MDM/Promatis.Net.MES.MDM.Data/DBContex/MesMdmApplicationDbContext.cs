using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.MDM.Domain;

namespace Promatis.Net.MES.MDM.Data;

public class MesMdmApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration)
    : MesApplicationDbContext(options, configuration)
{
    public DbSet<TechnologicalOperation> TechnologicalOperations => Set<TechnologicalOperation>();
    public DbSet<TechnologicalOperationUnit> TechnologicalOperationUnits => Set<TechnologicalOperationUnit>();
    public DbSet<TechnologicalParameter> TechnologicalParameters => Set<TechnologicalParameter>();
    public DbSet<TechnologicalOperationParameter> TechnologicalOperationParameters => Set<TechnologicalOperationParameter>();
}