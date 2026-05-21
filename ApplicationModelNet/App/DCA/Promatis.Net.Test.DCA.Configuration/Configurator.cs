using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration.Web;
using Promatis.Net.Data;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.DCA.Data;
using Promatis.Net.Test.DCA.Data;

namespace Promatis.Net.Test.DCA.Configuration;

public class Configurator : IWebAppConfigurator
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Регистрация единственной реальной фабрики БД
        services.AddDbContextFactory<DcaApplicationDbContext>((sp, options) =>
        {
            string? baseConnString = configuration.GetConnectionString("DefaultConnection");

            var builder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnString)
            {
                Username = "dca",
                Password = "dca"
            };

            options.UseNpgsql(builder.ConnectionString, x =>
                x.MigrationsAssembly("Promatis.Net.Test.DCA.DataInit"));
        });

        // 2. Адаптер для слоя MesDCA (IDbContextFactory<MesDcaApplicationDbContext>)
        services.AddScoped<IDbContextFactory<MesDcaApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<DcaApplicationDbContext>>();
            return new MesDcaDbContextFactoryAdapter(factory);
        });

        // 3. АДАПТЕР ДЛЯ СЛОЯ Mes (IDbContextFactory<MesApplicationDbContext>)
        services.AddScoped<IDbContextFactory<MesApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<DcaApplicationDbContext>>();
            return new MesDbContextFactoryAdapter(factory);
        });

        // 4. Адаптер для самого базового слоя Core (IDbContextFactory<ApplicationDbContext>)
        services.AddScoped<IDbContextFactory<ApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<DcaApplicationDbContext>>();
            return new DbContextFactoryAdapter(factory);
        });
    }

    public void ConfigureApp(IHost app)
    {
        // Например, проверка начальных данных для MDM
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        // Конфигурация промежуточного слоя
    }

    // --- КЛАССЫ АДАПТЕРОВ ---

    // Адаптер для MesMDM
    private class MesDcaDbContextFactoryAdapter(IDbContextFactory<DcaApplicationDbContext> factory)
        : IDbContextFactory<MesDcaApplicationDbContext>
    {
        public MesDcaApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }

    // Адаптер для Mes
    private class MesDbContextFactoryAdapter(IDbContextFactory<DcaApplicationDbContext> factory)
        : IDbContextFactory<MesApplicationDbContext>
    {
        public MesApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }

    // Адаптер для Core
    private class DbContextFactoryAdapter(IDbContextFactory<DcaApplicationDbContext> factory)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }
}