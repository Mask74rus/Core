using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class TreeTriggerChainTests
{
    // 1. Тестовая сущность дерева (наследуем от ReferenceTreeBase для ParentId и SoftDelete)
    private class TreeIntegrationNode : ReferenceTreeBase<TreeIntegrationNode> { }

    // Контекст с регистрацией сущности
    private class TreeIntegrationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TreeIntegrationNode>();
        }
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenParentIsSoftDeleted_ThroughChain()
    {
        // --- ARRANGE ---
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var triggerService = serviceProvider.GetRequiredService<DatabaseTriggerService>();

        // Регистрируем наш древовидный триггер
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, serviceProvider))
            .Options;

        await using var context = new TreeIntegrationDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // 1. Создаем родителя и помечаем его как удаленного
        var deadParent = new TreeIntegrationNode
        {
            Name = "Dead Parent",
            DeletedAt = DateTime.UtcNow
        };
        context.Add(deadParent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 2. Создаем дочерний элемент, ссылающийся на удаленного родителя
        var child = new TreeIntegrationNode
        {
            Name = "Child",
            ParentId = deadParent.Id
        };
        context.Add(child);

        // --- ACT & ASSERT ---

        // Ожидаем, что цепочка Interceptor -> TriggerService -> ReferenceTreeParentTrigger
        // заблокирует сохранение из-за того, что родитель "удален"
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        Assert.Equal("Нельзя назначить родителем удаленный объект.", exception.Message);
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenSelfParenting_ThroughChain()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var triggerService = serviceProvider.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, serviceProvider))
            .Options;

        await using var context = new TreeIntegrationDbContext(options);

        var node = new TreeIntegrationNode { Name = "Self" };
        node.ParentId = node.Id; // Самоцитирование
        context.Add(node);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Объект не может быть родителем самому себе.", exception.Message);
    }
}