using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Promatis.Net.Data.Init;

/// <summary>
/// Класс для получения данных необходимых миграции без запуска приложения
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Загружаем конфигурацию из appsettings.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");

        // 2. Настраиваем подключение и указываем сборку с миграциями
        optionsBuilder.UseNpgsql(connectionString, x =>
            x.MigrationsAssembly("Promatis.Net.Data.Init"));

        // 3. Передаем и опции, и саму конфигурацию в конструктор контекста
        // Теперь ApplicationDbContext сможет прочитать DatabaseSettings:DefaultSchema
        return new ApplicationDbContext(optionsBuilder.Options, configuration);
    }
}