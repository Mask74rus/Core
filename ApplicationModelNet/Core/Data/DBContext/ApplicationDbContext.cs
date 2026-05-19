using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain;

namespace Promatis.Net.Data;

public class ApplicationDbContext(
    DbContextOptions options,
    IConfiguration configuration,
    IServiceProvider? serviceProvider = null)
    : DbContext(options)
{
    protected virtual string Schema => "public";
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyModuleConfigurations(this);
        modelBuilder.ApplyGlobalConventions();
        base.OnModelCreating(modelBuilder);
    }

    // ИСПРАВЛЕННЫЙ И НАДЕЖНЫЙ ВАРИАНТ
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // 1. Проверяем, что мы в рантайме, а не в процессе миграции CLI
        if (!EF.IsDesignTime && serviceProvider != null)
        {
            DatabaseTriggerInterceptor? interceptor = null;

            try
            {
                // Попытка 1: Пробуем разрешить напрямую (если провайдер оказался Scoped)
                interceptor = serviceProvider.GetService(typeof(DatabaseTriggerInterceptor)) as DatabaseTriggerInterceptor;
            }
            catch (InvalidOperationException)
            {
                // Попытка 2: Если провайдер корневой (Root), создаем Scope вручную!
                // Это гарантирует, что Scoped-интерцептор разрешится без ошибок о Root-провайдере
                var scopeFactory = serviceProvider.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;

                if (scopeFactory != null)
                {
                    // Создаем временную область видимости
                    using IServiceScope scope = scopeFactory.CreateScope();
                    interceptor = scope.ServiceProvider.GetService(typeof(DatabaseTriggerInterceptor)) as DatabaseTriggerInterceptor;
                }
            }

            // Если интерцептор успешно найден одним из способов — подключаем его
            if (interceptor != null)
            {
                optionsBuilder.AddInterceptors(interceptor);
            }
        }
    }
}
