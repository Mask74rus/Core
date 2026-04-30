using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class AuditTriggerTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ (вынесены из методов) ---

    private class AuditIntegrationEntity : DomainObject, IAudit
    {
        public string Name { get; set; } = "";
    }

    // Специальный контекст, знающий про логи и тестовую сущность
    private class AuditIntegrationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AuditIntegrationEntity>();
            modelBuilder.Entity<AuditLog>();
        }
    }

    // --- 2. САМ ТЕСТ ---

    [Fact]
    public async Task SaveChanges_ShouldCreateAuditLog_WithCorrectJson()
    {
        // --- ARRANGE ---
        var services = new ServiceCollection();
        string dbName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // ИСПРАВЛЕНИЕ: Настраиваем фабрику так, чтобы она создавала НОВЫЙ экземпляр при каждом вызове
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .Returns(() => Task.FromResult<ApplicationDbContext>(new AuditIntegrationDbContext(options)));

        services.AddLogging();
        services.AddSingleton(factoryMock.Object);
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var triggerService = serviceProvider.GetRequiredService<DatabaseTriggerService>();

        triggerService.Register<DomainObject, AuditTrigger>();

        DbContextOptions<ApplicationDbContext> mainOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, serviceProvider))
            .Options;

        // Основной контекст для операции
        await using var context = new AuditIntegrationDbContext(mainOptions);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // --- ACT ---
        var entity = new AuditIntegrationEntity { Name = "Audit Test" };
        context.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // --- ASSERT ---
        // Здесь мы снова вызываем фабрику, которая создаст еще один (уже третий) чистый контекст
        await using ApplicationDbContext checkContext = await factoryMock.Object.CreateDbContextAsync();
        AuditLog? log = await checkContext.Set<AuditLog>()
            .FirstOrDefaultAsync(x => x.EntityId == entity.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(log);
        Assert.Equal("Added", log.Action);
        Assert.Contains("Audit Test", log.ChangesJson);
    }
}