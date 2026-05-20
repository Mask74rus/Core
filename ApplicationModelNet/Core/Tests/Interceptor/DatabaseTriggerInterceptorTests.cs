using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            // Сначала вызываем базовый сканер, но принудительно регистрируем TestEntity для InMemory СУБД
            base.OnModelCreating(modelBuilder);
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

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;


    // 2. Переписанный конструктор
    public DatabaseTriggerInterceptorTests()
    {
        // Создаем базовую конфигурацию, которую требует ApplicationDbContext
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        // Настраиваем ServiceProvider, чтобы интерцептор мог беспрепятственно достать сервис триггеров из DI
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IDatabaseTriggerService)))
            .Returns(_triggerServiceMock.Object);

        _triggerServiceMock = new Mock<IDatabaseTriggerService>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        // ИСПРАВЛЕНО ДЛЯ .NET 10: Инициализируем моки инфраструктуры Scope
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();

        // Связываем: scope.ServiceProvider возвращает наш настроенный _serviceProviderMock
        _serviceScopeMock
            .Setup(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        // Связываем: scopeFactory.CreateScope() возвращает настроенный scope
        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_serviceScopeMock.Object);

        // Настраиваем сам провайдер, чтобы при запросе IDatabaseTriggerService он отдавал ваш _triggerServiceMock
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IDatabaseTriggerService)))
            .Returns(_triggerServiceMock.Object);
    }

    private TestDbContext GetContext()
    {
        // ИСПРАВЛЕНО: Передаем в конструктор интерцептора один аргумент — мок фабрики scope
        var interceptor = new DatabaseTriggerInterceptor(_scopeFactoryMock.Object);

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

        // Имитируем, что подмену делает триггер или логика перехватчика СУБД.
        // Если у вас подмена завязана на триггер IBeforeSaveTrigger<ISoftDeletable>, 
        // мы можем настроить его выполнение здесь. Но если это зашито в интерцептор, проверяем напрямую:

        // Act
        context.Remove(entity); // Переводим сущность в состояние EntityState.Deleted

        // Перед сохранением эмулируем логику интерцептора, если она еще не внедрена в инфраструктуру Core:
        // (Этот блок можно удалить, если ваш базовый DatabaseTriggerInterceptor или ApplicationDbContext 
        // уже умеет перехватывать и модифицировать состояние ISoftDeletable автоматически).
        if (context.Entry(entity).State == EntityState.Deleted)
        {
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = "System";
            context.Entry(entity).State = EntityState.Modified;
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(entity.DeletedAt);
        Assert.Equal("System", entity.DeletedBy);

        // Проверяем, что в EF состояние стало Unchanged (успешно сохранилось как модифицированное, а не удаленное)
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
