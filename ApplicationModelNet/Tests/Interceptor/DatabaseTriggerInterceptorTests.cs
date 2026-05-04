using Microsoft.EntityFrameworkCore;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Interceptor;

public class DatabaseTriggerInterceptorTests
{
    private class TestDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Регистрируем тестовую сущность в модели EF
            modelBuilder.Entity<TestEntity>();
        }
    }

    public class TestEntity : DomainObject, ISoftDeletable
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private readonly Mock<IDatabaseTriggerService> _triggerServiceMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    private TestDbContext GetContext()
    {
        var interceptor = new DatabaseTriggerInterceptor(_triggerServiceMock.Object, _serviceProviderMock.Object);
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        // Используем наш тестовый контекст с зарегистрированной сущностью
        return new TestDbContext(options);
    }

    [Fact]
    public async Task SavingChanges_ShouldConvertDeleteToSoftDelete()
    {
        // Arrange
        await using ApplicationDbContext context = GetContext();
        var entity = new TestEntity { Name = "To be deleted" };
        context.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken); // Сначала сохраняем

        // Act
        context.Remove(entity); // Пытаемся удалить физически
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal("System", entity.DeletedBy);
        // Проверяем, что состояние в базе осталось Modified (из-за переключения в интерцепторе)
        Assert.Equal(EntityState.Unchanged, context.Entry(entity).State);
    }

    [Fact]
    public async Task SavingChanges_ShouldCaptureAddedState()
    {
        // Arrange
        await using ApplicationDbContext context = GetContext();
        var entity = new TestEntity { Name = "New Entity" };
        context.Add(entity);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Проверяем, что ValidateAsync был вызван с состоянием Added
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
        await using ApplicationDbContext context = GetContext();
        var entity = new TestEntity { Name = "Initial" };
        context.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        entity.Name = "Updated";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        // Проверяем, что после успешного сохранения вызвалось уведомление
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
        await using ApplicationDbContext context = GetContext();
        var entity = new TestEntity { Name = "Invalid" };
        context.Add(entity);

        _triggerServiceMock
            .Setup(s => s.ValidateAsync(It.IsAny<object>(), It.IsAny<EntityStateChangeEnum>(), It.IsAny<List<PropertyChangeInfo>>(), It.IsAny<DbContext>()))
            .ThrowsAsync(new OperationCanceledException("Validation Error"));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
    }
}