using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Tests.Operation;

public class OperationTestDbContext : DbContext
{
    public OperationTestDbContext(DbContextOptions<OperationTestDbContext> options) : base(options) { }

    // Используем базовые типы, чтобы триггеры могли делать к ним запросы Set<T>()
    public DbSet<TechnologicalOperationBase> Operations => Set<TechnologicalOperationBase>();
    public DbSet<TechnologicalOperationUnitBase> OperationUnits => Set<TechnologicalOperationUnitBase>();
    public DbSet<Domain.UnitBase> Units => Set<Domain.UnitBase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настраиваем плоские таблицы для тестов, имитируя конвенции EF Core
        modelBuilder.Entity<TestOperation>().ToTable("TestOperations");
        modelBuilder.Entity<TestOperationUnit>().ToTable("TestOperationUnits");
        modelBuilder.Entity<TestUnitNode>().ToTable("TestUnitNodes");
    }
}