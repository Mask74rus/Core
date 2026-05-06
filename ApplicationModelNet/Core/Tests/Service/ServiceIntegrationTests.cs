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

    public class IntegratedService(IDbContextFactory<ApplicationDbContext> f)
        : BaseService<IntegratedEntity, Guid>(f);

    // --- 2. НАСТРОЙКА КОНТЕКСТА ---
    // Нам нужно добавить сущность в модель, иначе EF её не увидит
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

        // ВАЖНО: Регистрируем на DomainObject, так как AuditTrigger 
        // реализует IAfterSaveTrigger<DomainObject>
        triggerService.Register<DomainObject, AuditTrigger>();

        // Перенастраиваем фабрику для использования нашего расширенного контекста с IntegratedEntity
        // (Опционально, если IntegratedEntity не попал в общую базу через сканер)

        var service = new IntegratedService(Factory);
        var entityId = Guid.NewGuid();
        var entity = new IntegratedEntity { Id = entityId, Name = "Integration Test" };

        // Act
        await service.AddAsync(entity);

        // Assert
        // Ждем немного, так как триггеры могут работать асинхронно (если это заложено в логике Notify)
        await Task.Delay(50, TestContext.Current.CancellationToken);

        await using ApplicationDbContext context = await Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        // Проверяем лог аудита
        AuditLog? auditLog = await context.Set<AuditLog>()
            .FirstOrDefaultAsync(x => x.EntityId == entityId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(auditLog);
        Assert.Equal("Added", auditLog.Action);
        Assert.Contains("Integration Test", auditLog.ChangesJson);
    }
}
