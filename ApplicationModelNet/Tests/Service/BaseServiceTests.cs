using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using System.Text.Json;

namespace Promatis.Net.ApplicationModel.Tests.Service;

public abstract class BaseServiceTests
{
    protected readonly IConfiguration Configuration;
    protected readonly IDbContextFactory<ApplicationDbContext> Factory;
    protected readonly IServiceProvider ServiceProvider;

    protected BaseServiceTests()
    {
        // 1. Конфигурация
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // 2. Сначала создаем мок фабрики
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        Factory = factoryMock.Object;

        // 3. Настройка DI (Теперь добавляем туда Factory)
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Configuration);
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(Factory);

        // Регистрируем сервисы инфраструктуры триггеров
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        // ВАЖНО: Регистрируем ВСЕ триггеры, которые могут быть вызваны
        services.AddScoped<AuditTrigger>();
        services.AddScoped<FluentValidationTrigger>(); // Добавляем этот
        services.AddScoped<ReferenceTreeParentTrigger>(); // И этот, если используется в иерархии

        // Также для FluentValidationTrigger нужны валидаторы
        // Если в тестах используются реальные валидаторы, можно вызвать сканер:
        // services.AddValidatorsFromAssemblyContaining<SomeValidator>();

        ServiceProvider = services.BuildServiceProvider();

        // 4. Настраиваем поведение мока (Returns должен быть в конце, когда всё готово)
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                IServiceScope scope = ServiceProvider.CreateScope();
                var triggerService = scope.ServiceProvider.GetRequiredService<IDatabaseTriggerService>();

                DbContextOptions<ApplicationDbContext> interceptorOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, scope.ServiceProvider))
                    .Options;

                return Task.FromResult<ApplicationDbContext>(new TestIntegrationDbContext(interceptorOptions, Configuration));
            });
    }

    // ВЫНОСИМ КЛАСС СЮДА, чтобы он был доступен всем наследникам
    protected class TestIntegrationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration config)
        : ApplicationDbContext(options, config)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Добавьте эту строку здесь:
            modelBuilder.Entity<ServiceIntegrationTests.IntegratedEntity>();

            // Остальные регистрации
            modelBuilder.Entity<BaseServiceTests_Crud.TestEntity>();
            modelBuilder.Entity<ReferenceServiceTests_Search.TestRef>();
        }
    }
}