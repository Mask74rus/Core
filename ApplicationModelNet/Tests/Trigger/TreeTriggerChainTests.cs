using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

// --- УНИКАЛЬНЫЕ СУЩНОСТИ ДЛЯ ИЗОЛЯЦИИ ТЕСТОВ ---
public class SoftDeleteNode : ReferenceTreeBase<SoftDeleteNode> { }
public class SelfParentNode : ReferenceTreeBase<SelfParentNode> { }

// Общий контекст для интеграционных тестов
public class TreeTestDbContext(DbContextOptions<ApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SoftDeleteNode>();
        modelBuilder.Entity<SelfParentNode>();
    }
}

public class TreeTriggerChainTests
{
    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // 1. Основные сервисы
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        // 2. РЕГИСТРИРУЕМ ВСЕ ТРИГГЕРЫ (Защита от статики)
        // Добавляем FluentValidationTrigger, чтобы тесты не падали, если он остался в статике
        services.AddScoped<FluentValidationTrigger>();
        services.AddScoped<AuditTrigger>();
        services.AddScoped<ReferenceTreeParentTrigger>();

        // 3. Зависимости триггеров
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Мок фабрики для AuditTrigger
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        services.AddSingleton(factoryMock.Object);

        // 4. Заглушки для иерархических интерфейсов (если были Register<IAudit, ...>)
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IAudit>>().Object);

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenParentIsSoftDeleted_ThroughChain()
    {
        // --- ARRANGE ---
        using var serviceProvider = CreateProvider();
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();

        // Регистрируем системный триггер для интерфейса
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        await using var context = new TreeTestDbContext(options);

        // 1. Создаем "удаленного" родителя
        var deadParent = new SoftDeleteNode
        {
            Id = Guid.NewGuid(),
            Name = "Dead Parent",
            DeletedAt = DateTime.UtcNow
        };
        context.Add(deadParent);
        await context.SaveChangesAsync();

        // 2. Ребенок
        var child = new SoftDeleteNode
        {
            Name = "Child",
            ParentId = deadParent.Id
        };
        context.Add(child);

        // --- ACT & ASSERT ---
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenSelfParenting_ThroughChain()
    {
        // --- ARRANGE ---
        using var serviceProvider = CreateProvider();
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        await using var context = new TreeTestDbContext(options);

        var node = new SelfParentNode { Id = Guid.NewGuid(), Name = "Self" };
        node.ParentId = node.Id;
        context.Add(node);

        // --- ACT & ASSERT ---
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await context.SaveChangesAsync());

        Assert.Equal("Объект не может быть родителем самому себе.", exception.Message);
    }
}