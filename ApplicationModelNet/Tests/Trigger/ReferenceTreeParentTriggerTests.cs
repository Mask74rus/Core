using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class ReferenceTreeParentTriggerTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ ---

    private class TestNode : ReferenceTreeBase<TestNode> { }

    // Контекст теперь принимает IConfiguration
    private class TestDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IConfiguration configuration)
        : ApplicationDbContext(options, configuration)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestNode>();
        }
    }

    // --- 2. ПОДГОТОВКА ОКРУЖЕНИЯ ---

    private readonly IConfiguration _configuration;

    public ReferenceTreeParentTriggerTests()
    {
        // Создаем фейковый конфиг для тестов
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();
    }

    private async Task<TestDbContext> GetContextAsync()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options, _configuration);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    // --- 3. ТЕСТЫ ---

    [Fact]
    public async Task Trigger_ShouldCancel_WhenObjectIsItsOwnParent()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        var nodeId = Guid.NewGuid();
        // Id и ParentId совпадают
        var node = new TestNode { Id = nodeId, ParentId = nodeId, Name = "Self Parent" };

        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            node, EntityStateChangeEnum.Added, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Equal("Объект не может быть родителем самому себе.", args.ErrorMessage);
    }

    [Fact]
    public async Task Trigger_ShouldCancel_WhenParentDoesNotExistInDatabase()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        var missingParentId = Guid.NewGuid();
        var node = new TestNode { Name = "Orphan Node", ParentId = missingParentId };

        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            node, EntityStateChangeEnum.Added, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Contains("не найден", args.ErrorMessage);
    }

    [Fact]
    public async Task Trigger_ShouldNotCancel_WhenParentExistsInDatabase()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        var parent = new TestNode { Name = "Valid Parent" };
        context.Add(parent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new TestNode { Name = "Child Node", ParentId = parent.Id };
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            child, EntityStateChangeEnum.Added, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.False(args.Cancel);
        Assert.Null(args.ErrorMessage);
    }

    [Fact]
    public async Task Trigger_ShouldCancel_WhenParentIsSoftDeleted()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        var parent = new TestNode
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Parent",
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "Admin"
        };
        context.Add(parent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new TestNode { Name = "Child of Dead", ParentId = parent.Id };
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            child, EntityStateChangeEnum.Added, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Equal("Нельзя назначить родителем удаленный объект.", args.ErrorMessage);
    }
}
