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

public class IntegrationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IConfiguration configuration)
    : ApplicationDbContext(options, configuration)
{
    // Объявляем приватный тестовый узел прямо внутри контекста или файла тестов
    private class TestNode : ReferenceTreeBase { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. Запускаем базовый автоматический сканер Promatis
        base.OnModelCreating(modelBuilder);

        // 2. ИСПРАВЛЕНИЕ: Для InMemory провайдера просто регистрируем сущность как самостоятельную.
        // Больше никаких HasBaseType и дискриминаторов, так как ReferenceTreeBase глобально проигнорирован!
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<TestNode>(builder =>
            {
                // Задаем явное имя таблицы, чтобы InMemory изолировал её
                builder.ToTable("FullTriggerChain_TestNodes");

                // Явно указываем типы связей Родитель-Потомок через базовый класс дерева
                builder.HasOne(typeof(ReferenceTreeBase), nameof(ReferenceTreeBase.Parent))
                    .WithMany(nameof(ReferenceTreeBase.Children))
                    .HasForeignKey(nameof(ReferenceTreeBase.ParentId));
            });

            // Также регистрируем саму тестовую интеграционную сущность
            modelBuilder.Entity<IntegrationEntity>();
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