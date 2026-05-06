using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Promatis.Net.Data;

public partial class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IConfiguration configuration)
    : DbContext(options)
{
    private readonly string _schema = configuration.GetSection("DatabaseSettings:DefaultSchema").Value ?? "public";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Указываем общую схему
        modelBuilder.HasDefaultSchema(_schema);

        // АВТОМАТИКА: Находим все классы IEntityTypeConfiguration в этой сборке
        // Сканируем все загруженные сборки Promatis.* на наличие конфигураций
        IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith("Promatis."));

        foreach (Assembly assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        // ГЛОБАЛЬНЫЕ ПРАВИЛА: Применяем индексы и настройки ключей через метод расширения
        modelBuilder.ApplyGlobalConventions();

        base.OnModelCreating(modelBuilder);
    }
}