using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.MES.Tests.UnitBase;

// Тестовый контекст внутри пространства имен тестов
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<TestUnit> Units => Set<TestUnit>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<TestUnit>().ToTable("TestUnits");
}