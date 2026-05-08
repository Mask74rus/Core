using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.DataInit;

/// <summary>
/// Класс для получения данных необходимых миграции без запуска приложения
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppApplicationDbContext>
{
    public AppApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Указываем путь к appsettings.json в лаунчере
        string basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Promatis.Net.Launcher.Web"));

        if (!Directory.Exists(basePath)) basePath = Directory.GetCurrentDirectory();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // 2. Чистая строка подключения для MDM
        var connBuilder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"))
        {
            Username = "mdm",
            Password = "mdm"
        };

        var optionsBuilder = new DbContextOptionsBuilder<AppApplicationDbContext>();
        optionsBuilder.UseNpgsql(connBuilder.ConnectionString, x =>
            x.MigrationsAssembly("Promatis.Net.Test.MDM.DataInit"));

        // НИКАКИХ .AddInterceptors() здесь быть не должно!
        return new AppApplicationDbContext(optionsBuilder.Options, configuration);
    }
}