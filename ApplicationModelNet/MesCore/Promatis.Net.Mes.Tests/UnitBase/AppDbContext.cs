using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.MES.Tests.UnitBase;

// Тестовый контекст внутри пространства имен тестов
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Используем базовый тип, чтобы триггер мог делать Set<UnitBase>()
    public DbSet<Domain.UnitBase> Units => Set<Domain.UnitBase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Настраиваем TPT (как в основном проекте)
        modelBuilder.Entity<Domain.UnitBase>().UseTptMappingStrategy();

        // 2. Указываем, что TestUnit — это наследник
        modelBuilder.Entity<TestUnit>().ToTable("TestUnits");
    }
}