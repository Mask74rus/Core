using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

// --- УНИКАЛЬНЫЕ СУЩНОСТИ ДЛЯ ИЗОЛЯЦИИ ТЕСТОВ ---
public class SoftDeleteNode : ReferenceTreeBase { }
public class SelfParentNode : ReferenceTreeBase { }

// Общий контекст для интеграционных тестов (теперь с IConfiguration)
public class TreeTestDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IConfiguration configuration)
    : ApplicationDbContext(options, configuration)
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
    private (ServiceProvider Provider, IConfiguration Config) CreateProviderAndConfig()
    {
        // Создаем фейковую конфигурацию
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration); // Добавляем конфиг

        // 1. Основные сервисы
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        // 2. РЕГИСТРИРУЕМ ВСЕ ТРИГГЕРЫ
        services.AddScoped<FluentValidationTrigger>();
        services.AddScoped<AuditTrigger>();
        services.AddScoped<ReferenceTreeParentTrigger>();

        // 3. Зависимости триггеров
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        services.AddSingleton(factoryMock.Object);

        // 4. Заглушки для интерфейсов
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IAudit>>().Object);

        return (services.BuildServiceProvider(), configuration);
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenParentIsSoftDeleted_ThroughChain()
    {
        // --- ARRANGE ---
        (ServiceProvider serviceProvider, IConfiguration configuration) = CreateProviderAndConfig();
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        // Передаем опции и конфиг
        await using var context = new TreeTestDbContext(options, configuration);

        // 1. Создаем "удаленного" родителя
        var deadParent = new SoftDeleteNode
        {
            Id = Guid.NewGuid(),
            Name = "Dead Parent",
            DeletedAt = DateTime.UtcNow
        };
        context.Add(deadParent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

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
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task SaveChanges_ShouldCancel_WhenSelfParenting_ThroughChain()
    {
        // --- ARRANGE ---
        (ServiceProvider serviceProvider, IConfiguration configuration) = CreateProviderAndConfig();
        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<IDomainObjectHasKey<Guid>, ReferenceTreeParentTrigger>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        await using var context = new TreeTestDbContext(options, configuration);

        var node = new SelfParentNode { Id = Guid.NewGuid(), Name = "Self" };
        node.ParentId = node.Id;
        context.Add(node);

        // --- ACT & ASSERT ---
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await context.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Объект не может быть родителем самому себе.", exception.Message);
    }
}