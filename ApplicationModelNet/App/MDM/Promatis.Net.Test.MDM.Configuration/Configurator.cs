using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration.Web;
using Promatis.Net.Data;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.MDM.Data;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.Configuration;

public class Configurator : IWebAppConfigurator
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // 1. Регистрация единственной реальной фабрики БД
        services.AddDbContextFactory<MdmApplicationDbContext>((sp, options) =>
        {
            string? baseConnString = configuration.GetConnectionString("DefaultConnection");

            var builder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnString)
            {
                Username = "mdm",
                Password = "mdm"
            };

            options.UseNpgsql(builder.ConnectionString, x =>
                x.MigrationsAssembly("Promatis.Net.Test.MDM.DataInit"));
        });

        // 2. Адаптер для слоя MesMDM (IDbContextFactory<MesMdmApplicationDbContext>)
        services.AddScoped<IDbContextFactory<MesMdmApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<MdmApplicationDbContext>>();
            return new MesMdmDbContextFactoryAdapter(factory);
        });

        // 3. АДАПТЕР ДЛЯ СЛОЯ Mes (IDbContextFactory<MesApplicationDbContext>)
        services.AddScoped<IDbContextFactory<MesApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<MdmApplicationDbContext>>();
            return new MesDbContextFactoryAdapter(factory);
        });

        // 4. Адаптер для самого базового слоя Core (IDbContextFactory<ApplicationDbContext>)
        services.AddScoped<IDbContextFactory<ApplicationDbContext>>(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<MdmApplicationDbContext>>();
            return new DbContextFactoryAdapter(factory);
        });
    }

    public void ConfigureApp(IHost app) { }

    public void ConfigureMiddleware(WebApplication app) { }

    // --- КЛАССЫ АДАПТЕРОВ ---

    // Адаптер для MesMDM
    private class MesMdmDbContextFactoryAdapter(IDbContextFactory<MdmApplicationDbContext> factory)
        : IDbContextFactory<MesMdmApplicationDbContext>
    {
        public MesMdmApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }

    // Адаптер для Mes
    private class MesDbContextFactoryAdapter(IDbContextFactory<MdmApplicationDbContext> factory)
        : IDbContextFactory<MesApplicationDbContext>
    {
        public MesApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }

    // Адаптер для Core
    private class DbContextFactoryAdapter(IDbContextFactory<MdmApplicationDbContext> factory)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }
}

