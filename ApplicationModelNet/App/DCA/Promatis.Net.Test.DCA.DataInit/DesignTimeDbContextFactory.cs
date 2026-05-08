using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Promatis.Net.Test.DCA.Data;

namespace Promatis.Net.Test.DCA.DataInit;

/// <summary>
/// Класс для получения данных необходимых миграции без запуска приложения
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DcaApplicationDbContext>
{
    public DcaApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Указываем путь к appsettings.json в лаунчере
        string basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Promatis.Net.Launcher.Web"));

        if (!Directory.Exists(basePath)) basePath = Directory.GetCurrentDirectory();

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        // 2. Чистая строка подключения для DCA
        var connBuilder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"))
        {
            Username = "dca",
            Password = "dca"
        };

        var optionsBuilder = new DbContextOptionsBuilder<DcaApplicationDbContext>();
        optionsBuilder.UseNpgsql(connBuilder.ConnectionString, x =>
            x.MigrationsAssembly("Promatis.Net.Test.DCA.DataInit"));

        // НИКАКИХ .AddInterceptors() здесь быть не должно!
        return new DcaApplicationDbContext(optionsBuilder.Options, configuration);
    }
}