using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Interceptor;

public class DatabaseTriggerInterceptorTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ ---

    private class TestDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration configuration)
        : ApplicationDbContext(options, configuration)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Регистрируем тестовую сущность для InMemory
            modelBuilder.Entity<TestEntity>();
        }
    }

    public class TestEntity : DomainObject, ISoftDeletable
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    // --- 2. ПОДГОТОВКА ОКРУЖЕНИЯ ---

    private readonly Mock<IDatabaseTriggerService> _triggerServiceMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly IConfiguration _configuration;

    public DatabaseTriggerInterceptorTests()
    {
        // Создаем конфигурацию, которую требует базовый ApplicationDbContext
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        // Настраиваем ServiceProvider, чтобы интерцептор мог достать сервис, если нужно
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IDatabaseTriggerService)))
            .Returns(_triggerServiceMock.Object);
    }

    private TestDbContext GetContext()
    {
        var interceptor = new DatabaseTriggerInterceptor(_triggerServiceMock.Object, _serviceProviderMock.Object);

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestDbContext(options, _configuration);
    }

    // --- 3. ТЕСТЫ ---

    [Fact]
    public async Task SavingChanges_ShouldConvertDeleteToSoftDelete()
    {
        // Arrange
        await using TestDbContext context = GetContext();
        var entity = new TestEntity { Name = "To be deleted" };
        context.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        context.Remove(entity); // Пытаемся физически удалить
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal("System", entity.DeletedBy);
        // Проверяем, что в EF состояние стало Unchanged (после подмены Deleted на Modified и сохранения)
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);
    }

    [Fact]
    public async Task SavingChanges_ShouldCaptureAddedState()
    {
        // Arrange
        await using TestDbContext context = GetContext();
        var entity = new TestEntity { Name = "New Entity" };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Проверяем, вызывался ли ValidateAsync в сервисе триггеров
        _triggerServiceMock.Verify(s => s.ValidateAsync(
            entity,
            EntityStateChangeEnum.Added,
            It.Is<List<PropertyChangeInfo>>(c => c.Any(p => p.PropertyName == "Name")),
            context),
        Times.Once);
    }

    [Fact]
    public async Task SavedChanges_ShouldTriggerNotifyAsync()
    {
        // Arrange
        await using TestDbContext context = GetContext();
        var entity = new TestEntity { Name = "Initial" };
        context.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        entity.Name = "Updated";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Проверяем, вызвалось ли уведомление ПОСЛЕ сохранения
        _triggerServiceMock.Verify(s => s.NotifyAsync(
            entity,
            EntityStateChangeEnum.Modified,
            It.Is<List<PropertyChangeInfo>>(c => c.Count == 1),
            "System",
            It.IsAny<DateTime>()),
        Times.Once);
    }

    [Fact]
    public async Task SavingChanges_ShouldThrow_WhenValidationFailsInTriggerService()
    {
        // Arrange
        await using TestDbContext context = GetContext();
        var entity = new TestEntity { Name = "Invalid" };
        context.Add(entity);

        // Имитируем ошибку валидации в сервисе триггеров
        _triggerServiceMock
            .Setup(s => s.ValidateAsync(It.IsAny<object>(), It.IsAny<EntityStateChangeEnum>(), It.IsAny<List<PropertyChangeInfo>>(), It.IsAny<DbContext>()))
            .ThrowsAsync(new OperationCanceledException("Validation Error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Validation Error", ex.Message);
    }
}
