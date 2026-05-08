using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;
using Promatis.Net.Test.MDM.Domain;

namespace Promatis.Net.Test.MDM.Data;

public class MdmApplicationDbContext(
    DbContextOptions<MdmApplicationDbContext> options,
    IConfiguration configuration)
    : ApplicationDbContext(options, configuration)
{

    // Указываем схему для этого конкретного контекста
    protected override string Schema => "mdm";

    public DbSet<UnitBase> Units => Set<UnitBase>();
    public DbSet<DepartmentUnit> DepartmentUnits => Set<DepartmentUnit>();
    public DbSet<ProductionUnit> ProductionUnits => Set<ProductionUnit>();
    public DbSet<TransportUnit> TransportUnits => Set<TransportUnit>();
    public DbSet<StorageUnit> StorageUnits => Set<StorageUnit>();
    public DbSet<PositionUnit> PositionUnits => Set<PositionUnit>();
}