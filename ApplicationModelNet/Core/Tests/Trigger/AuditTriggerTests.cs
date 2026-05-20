using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class AuditTriggerTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ ---

    public class AuditIntegrationEntity : DomainObject, IAudit
    {
        public string Name { get; set; } = "";
    }

    // ТЕСТОВЫЙ КОНТЕКСТ: Теперь принимает IConfiguration
    public class AuditIntegrationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration configuration)
        : ApplicationDbContext(options, configuration)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Регистрация сущностей для теста
            modelBuilder.Entity<AuditIntegrationEntity>();
            modelBuilder.Entity<AuditLog>();
        }
    }

    // --- 2. КОНСТРУКТОР ТЕСТА ---
    public AuditTriggerTests()
    {
        DatabaseTriggerService.ClearInternalRegistrations();
    }

    // --- 3. САМ ТЕСТ ---

    [Fact]
    public async Task SaveChanges_ShouldCreateAuditLog_WithCorrectJson()
    {
        // Создаем фейковую конфигурацию
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration); // Добавляем в DI
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        string dbName = "FinalAuditDb_" + Guid.NewGuid();
        DbContextOptions<ApplicationDbContext> dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Настройка фабрики (теперь пробрасываем конфиг)
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .Returns(() => Task.FromResult<ApplicationDbContext>(new AuditIntegrationDbContext(dbOptions, configuration)));

        services.AddSingleton(factoryMock.Object);
        services.AddScoped<AuditTrigger>();
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<DomainObject, AuditTrigger>();

        // Перехватчик
        // ИСПРАВЛЕНО ДЛЯ .NET 10: Извлекаем фабрику Scope из тестового контейнера зависимостей
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        // ИСПРАВЛЕНО: Создаем интерцептор, передавая ему только ОДИН аргумент — scopeFactory
        var interceptor = new DatabaseTriggerInterceptor(scopeFactory);

        DbContextOptions<ApplicationDbContext> mainOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        var entityId = Guid.NewGuid();

        // --- ACT & ASSERT ---
        // Передаем configuration в конструктор контекста
        await using (var context = new AuditIntegrationDbContext(mainOptions, configuration))
        {
            var entity = new AuditIntegrationEntity { Id = entityId, Name = "Audit Test" };
            context.Add(entity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Даем время триггеру
            await Task.Delay(100, TestContext.Current.CancellationToken);

            AuditLog? log = await context.Set<AuditLog>()
                .FirstOrDefaultAsync(x => x.EntityId == entityId, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(log);
            Assert.Equal("Added", log.Action);
            Assert.Contains("Audit Test", log.ChangesJson);
        }
    }
}