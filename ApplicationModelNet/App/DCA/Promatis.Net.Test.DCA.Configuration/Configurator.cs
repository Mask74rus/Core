using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration.Web;
using Promatis.Net.Data;
using Promatis.Net.Test.DCA.Data;

namespace Promatis.Net.Test.DCA.Configuration;

public class Configurator : IWebAppConfigurator
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Регистрируем фабрику контекста
        services.AddDbContextFactory<Data.DcaApplicationDbContext>((sp, options) =>
        {
            string? baseConnString = configuration.GetConnectionString("DefaultConnection");

            var builder = new Npgsql.NpgsqlConnectionStringBuilder(baseConnString)
            {
                Username = "dca",
                Password = "dca"
            };

            options.UseNpgsql(builder.ConnectionString, x =>
                x.MigrationsAssembly("Promatis.Net.Test.DCA.DataInit"));

            if (!EF.IsDesignTime)
            {
                var interceptor = sp.GetRequiredService<DatabaseTriggerInterceptor>();
                options.AddInterceptors(interceptor);
            }
        });

        // 2. Исправляем ошибку приведения: регистрируем фабрику для базового типа
        // Мы говорим DI: "Когда кто-то (например, AuditTrigger) просит фабрику базового контекста, 
        // возьми фабрику DCA и приведи созданный контекст к базовому типу".
        services.AddScoped<IDbContextFactory<Net.Data.ApplicationDbContext>>(sp =>
        {
            // Получаем фабрику DCA
            var factory = sp.GetRequiredService<IDbContextFactory<Data.DcaApplicationDbContext>>();

            // Оборачиваем её в универсальный адаптер
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

    private class DbContextFactoryAdapter(IDbContextFactory<Data.DcaApplicationDbContext> factory)
        : IDbContextFactory<Net.Data.ApplicationDbContext>
    {
        public Net.Data.ApplicationDbContext CreateDbContext() => factory.CreateDbContext();
    }
}