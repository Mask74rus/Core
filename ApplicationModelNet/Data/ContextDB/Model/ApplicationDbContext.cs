using Microsoft.EntityFrameworkCore;

namespace Promatis.Net.Data;

public partial class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Указываем общую схему
        modelBuilder.HasDefaultSchema("test");

        // АВТОМАТИКА: Находим все классы IEntityTypeConfiguration в этой сборке
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ГЛОБАЛЬНЫЕ ПРАВИЛА: Применяем индексы и настройки ключей через метод расширения
        modelBuilder.ApplyGlobalConventions();

        base.OnModelCreating(modelBuilder);
    }
}