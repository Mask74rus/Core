using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Testing.Platform.Services;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.Service;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Service;

public class ServiceIntegrationTests : BaseServiceTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ ---

    public class IntegratedEntity : DomainObject, IAudit
    {
        public string Name { get; set; } = "";
    }

    // Добавляем TContext (ApplicationDbContext) в наследование
    public class IntegratedService(IDbContextFactory<ApplicationDbContext> f)
        : BaseService<IntegratedEntity, Guid, ApplicationDbContext>(f);

    // --- 2. НАСТРОЙКА КОНТЕКСТА ---
    private class ServiceTestDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration config)
        : TestIntegrationDbContext(options, config)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<IntegratedEntity>();
        }
    }

    // --- 3. ТЕСТ ---

    [Fact]
    public async Task AddAsync_Should_TriggerAuditLogCreation()
    {
        // Arrange
        var triggerService = ServiceProvider.GetRequiredService<IDatabaseTriggerService>();

        // Регистрация триггера
        triggerService.Register<DomainObject, AuditTrigger>();

        // Используем базовую фабрику (Factory), так как IntegratedService 
        // теперь корректно типизирован под ApplicationDbContext
        var service = new IntegratedService(Factory);
        var entityId = Guid.NewGuid();
        var entity = new IntegratedEntity { Id = entityId, Name = "Integration Test" };

        // Act
        await service.AddAsync(entity);

        // Assert
        // Небольшая задержка для обработки триггеров
        await Task.Delay(50, TestContext.Current.CancellationToken);

        await using ApplicationDbContext context = await Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // Проверяем лог аудита
        AuditLog? auditLog = await context.Set<AuditLog>()
            .FirstOrDefaultAsync(x => x.EntityId == entityId, TestContext.Current.CancellationToken);

        Assert.NotNull(auditLog);
        Assert.Equal("Added", auditLog.Action);
        Assert.Contains("Integration Test", auditLog.ChangesJson);
    }
}
