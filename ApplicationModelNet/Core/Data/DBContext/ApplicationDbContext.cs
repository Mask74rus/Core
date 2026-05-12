using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Domain;
using System.Reflection;

namespace Promatis.Net.Data;

public class ApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration)
    : DbContext(options)
{
    protected virtual string Schema => "public";

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Указываем общую схему
        modelBuilder.HasDefaultSchema(Schema);

        // Вызываем наше расширение, передавая сборку текущего контекста
        modelBuilder.ApplyModuleConfigurations(GetType().Assembly);

        // ГЛОБАЛЬНЫЕ ПРАВИЛА: Применяем индексы и настройки ключей через метод расширения
        modelBuilder.ApplyGlobalConventions();

        base.OnModelCreating(modelBuilder);
    }
}