using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.Test.MDM.Domain;

namespace Promatis.Net.Test.MDM.Data;

public class MdmApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration,
    IServiceProvider? serviceProvider = null) // Принимаем опциональный параметр
    : MesMdmApplicationDbContext(options, configuration, serviceProvider)
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