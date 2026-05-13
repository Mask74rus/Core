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

    private class TestNode : ReferenceTreeBase { }

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

    [Fact]
    public async Task Trigger_ShouldCancel_WhenDirectCyclicDependencyDetected()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        // 1. Создаем и сохраняем родительский элемент А
        var nodeA = new TestNode { Id = Guid.NewGuid(), Name = "Node A" };
        context.Add(nodeA);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 2. Создаем и сохраняем дочерний элемент B (Родитель: А)
        var nodeB = new TestNode { Id = Guid.NewGuid(), Name = "Node B", ParentId = nodeA.Id };
        context.Add(nodeB);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 3. Имитируем попытку сделать узел B родителем для узла A (Петля: A -> B -> A)
        nodeA.ParentId = nodeB.Id;

        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            nodeA, EntityStateChangeEnum.Modified, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Equal("Циклическая зависимость: нельзя переместить родительский узел внутрь собственного дочернего поддерева.", args.ErrorMessage);
    }

    [Fact]
    public async Task Trigger_ShouldCancel_WhenDeepCyclicDependencyDetected()
    {
        // Arrange
        await using TestDbContext context = await GetContextAsync();
        var trigger = new ReferenceTreeParentTrigger();

        // Построим цепочку: Root -> Child -> SubChild
        var root = new TestNode { Id = Guid.NewGuid(), Name = "Root" };
        context.Add(root);

        var child = new TestNode { Id = Guid.NewGuid(), Name = "Child", ParentId = root.Id };
        context.Add(child);

        var subChild = new TestNode { Id = Guid.NewGuid(), Name = "SubChild", ParentId = child.Id };
        context.Add(subChild);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Имитируем попытку переместить самый верхний узел "Root" внутрь его глубокого потомка "SubChild"
        // Цепочка проверки пойдет вверх: SubChild (в БД) -> Child (в БД) -> Root (перемещаемый). 
        root.ParentId = subChild.Id;

        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            root, EntityStateChangeEnum.Modified, [], context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Equal("Циклическая зависимость: нельзя переместить родительский узел внутрь собственного дочернего поддерева.", args.ErrorMessage);
    }
}
