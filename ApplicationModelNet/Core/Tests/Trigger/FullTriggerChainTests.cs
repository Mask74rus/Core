using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Text.Json;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

// --- 1. ТЕСТОВЫЕ КЛАССЫ ---

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

// Теперь принимает IConfiguration
public class IntegrationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IConfiguration configuration)
    : ApplicationDbContext(options, configuration)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Сначала вызываем базовое сканирование (оно пропустит TestNode благодаря фильтру !t.IsNested)
        base.OnModelCreating(modelBuilder);

        // Если в текущем тестовом контексте используется TestNode, конфигурируем его изолированно
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<TestNode>(builder =>
            {
                // Явно задаем имя таблицы или дискриминатора, уникальное для этого файла тестов
                builder.HasBaseType<ReferenceTreeBase>();
                builder.HasDiscriminator<string>("Discriminator").HasValue("FullTriggerChain_TestNode");
            });
        }
    }
}

public class FullTriggerChainTests
{
    [Fact]
    public async Task SaveChanges_ShouldExecuteFullChain_AndCancelOnValidationError()
    {
        // --- ARRANGE ---

        // 1. Создаем фейковую конфигурацию
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultSchema"] = "test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration); // Добавляем конфиг в DI

        // 2. Инфраструктурные настройки
        services.AddSingleton(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        // Мок фабрики теперь возвращает контекст с конфигом
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        services.AddSingleton(factoryMock.Object);

        // 3. Регистрация сервисов триггеров
        services.AddScoped<DatabaseTriggerService>();
        services.AddScoped<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        // Сами триггеры
        services.AddScoped<FluentValidationTrigger>();
        services.AddScoped<ReferenceTreeParentTrigger>();
        services.AddScoped<AuditTrigger>();

        // 4. Заглушки для интерфейсов (чтобы цепочка не рвалась, если триггер требует общие интерфейсы)
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IAudit>>().Object);
        services.AddScoped(_ => new Mock<IBeforeSaveTrigger<IDomainObject>>().Object);

        // 5. Регистрация валидатора
        services.AddTransient<IValidator<IntegrationEntity>, IntegrationValidator>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        using IServiceScope scope = serviceProvider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;
        var triggerService = sp.GetRequiredService<DatabaseTriggerService>();

        // Привязываем FluentValidationTrigger ко всем сущностям с Guid-ключом
        triggerService.Register<IDomainObjectHasKey<Guid>, FluentValidationTrigger>();

        // Настройка опций БД с перехватчиком
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, sp))
            .Options;

        // Создаем контекст, передавая опции и конфиг
        await using var context = new IntegrationDbContext(options, configuration);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Создаем объект, который НЕ пройдет валидацию (Name пустой)
        var entity = new IntegrationEntity { Name = "" };
        context.Add(entity);

        // --- ACT & ASSERT ---

        // Ожидаем, что интерцептор вызовет триггер, тот вызовет валидатор, и будет выброшено исключение
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Проверяем, что дошли именно до нашего сообщения из IntegrationValidator
        Assert.Equal("NameRequired", exception.Message);
    }
}