using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.DataInit;

/// <summary>
/// Класс для получения данных необходимых миграции без запуска приложения
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppApplicationDbContext>
{
    public AppApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Конфигурация
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Важно: для CLI лучше Directory
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 2. Настройка Npgsql
        optionsBuilder.UseNpgsql(connectionString, x =>
            // Миграции будут храниться в этом же проекте (DataInit)
            x.MigrationsAssembly("Promatis.Net.Test.MDM.DataInit"));

        return new AppApplicationDbContext(optionsBuilder.Options, configuration);
    }
}