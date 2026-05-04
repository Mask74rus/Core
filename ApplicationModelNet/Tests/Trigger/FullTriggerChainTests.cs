using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

// --- 1. ТЕСТОВЫЕ КЛАССЫ (вынесены наружу для корректной работы инфраструктуры) ---

public class IntegrationEntity : DomainObject, IAudit
{
    public string Name { get; set; } = "";
}

public class IntegrationValidator : AbstractValidator<IntegrationEntity>
{
    public IntegrationValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
    }
}

public class IntegrationDbContext(DbContextOptions<ApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<IntegrationEntity>();
    }
}

public class FullTriggerChainTests
{
    [Fact]
    public async Task SaveChanges_ShouldExecuteFullChain_AndCancelOnValidationError()
    {
        // --- ARRANGE ---
        var services = new ServiceCollection();
        services.AddLogging();

        // 1. Инфраструктурные настройки
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        services.AddSingleton(new Mock<IDbContextFactory<ApplicationDbContext>>().Object);

        // 2. Регистрация ВСЕХ классов триггеров (защита от статики)
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());
        services.AddScoped<FluentValidationTrigger>();
        services.AddScoped<ReferenceTreeParentTrigger>();
        services.AddScoped<AuditTrigger>();

        // 3. Регистрация ИНТЕРФЕЙСОВ-заглушек (защита от иерархических регистраций из других тестов)
        // Это предотвращает ошибку "No service for type IBeforeSaveTrigger<IAudit>"
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IAudit>>().Object);
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IDomainObject>>().Object);

        // 4. Регистрация валидатора для нашей сущности
        services.AddTransient<IValidator<IntegrationEntity>, IntegrationValidator>();

        var serviceProvider = services.BuildServiceProvider();

        // Создаем Scope, который будет жить до конца теста
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();

        // Явно привязываем триггер к нашей сущности для теста
        triggerService.Register<IDomainObjectHasKey<Guid>, FluentValidationTrigger>();

        services.AddScoped<FluentValidationTrigger>();

        // Настройка опций БД с интерцептором
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        await using var context = new IntegrationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Создаем невалидный объект
        var entity = new IntegrationEntity { Name = "" };
        context.Add(entity);

        // --- ACT & ASSERT ---

        // Проверяем, что цепочка сработала и выбросила OperationCanceledException
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await context.SaveChangesAsync();
        });

        // Проверяем, что сообщение пришло именно от валидатора
        Assert.Equal("NameRequired", exception.Message);
    }
}