using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
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

    // ТЕСТОВЫЙ КОНТЕКСТ: Должен явно знать обо всех сущностях, иначе InMemory их не увидит
    public class AuditIntegrationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Сначала вызываем базовый, чтобы подтянулись стандартные настройки
            base.OnModelCreating(modelBuilder);

            // Явно регистрируем сущности для InMemory провайдера
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
        DatabaseTriggerService.ClearInternalRegistrations();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        string dbName = "FinalAuditDb_" + Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Настройка фабрики
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .Returns(() => Task.FromResult<ApplicationDbContext>(new AuditIntegrationDbContext(dbOptions)));

        services.AddSingleton(factoryMock.Object);
        services.AddScoped<AuditTrigger>();
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<DomainObject, AuditTrigger>();

        var interceptor = new DatabaseTriggerInterceptor(triggerService, sp);
        var mainOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        Guid entityId = Guid.NewGuid();

        // --- ACT & ASSERT ---
        // Используем один контекст на всё время теста, чтобы InMemory не "утекла"
        using (var context = new AuditIntegrationDbContext(mainOptions))
        {
            var entity = new AuditIntegrationEntity { Id = entityId, Name = "Audit Test" };
            context.Add(entity);
            await context.SaveChangesAsync();

            // Даем время триггеру (так как он может работать в Task)
            await Task.Delay(100);

            // Проверяем лог ПРЯМО ЗДЕСЬ, пока основной контекст жив
            var log = await context.Set<AuditLog>()
                .FirstOrDefaultAsync(x => x.EntityId == entityId);

            if (log == null)
            {
                // Если все еще null, ищем вообще любой лог в таблице
                var anyLog = await context.Set<AuditLog>().AnyAsync();
                Assert.True(anyLog, "Таблица логов пуста. Триггер не смог записать данные.");
                Assert.NotNull(log);
            }

            Assert.Equal("Added", log.Action);
            Assert.Contains("Audit Test", log.ChangesJson);
        }
    }
}