using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Data;
using Promatis.Net.Data.Init;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class FullTriggerChainTests
{
    // --- 1. ТЕСТОВЫЕ КЛАССЫ ---

    private class IntegrationEntity : DomainObject
    {
        public string Name { get; set; } = "";
    }

    private class IntegrationValidator : AbstractValidator<IntegrationEntity>
    {
        public IntegrationValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("NameRequired");
        }
    }

    // Локальный контекст для теста, чтобы EF знал про IntegrationEntity
    private class IntegrationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<IntegrationEntity>();
        }
    }

    // --- 2. КОД ТЕСТА ---

    [Fact]
    public async Task SaveChanges_ShouldExecuteFullChain_AndCancelOnValidationError()
    {
        // --- ARRANGE ---
        var services = new ServiceCollection();
        services.AddLogging();

        // Регистрируем DatabaseTriggerService и как класс, и как интерфейс
        // Это важно, так как FluentValidationTrigger требует класс в конструкторе
        services.AddSingleton<DatabaseTriggerService>();
        services.AddSingleton<IDatabaseTriggerService>(sp => sp.GetRequiredService<DatabaseTriggerService>());

        // Регистрируем валидатор для нашей тестовой сущности
        services.AddTransient<IValidator<IntegrationEntity>, IntegrationValidator>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Получаем сервис и регистрируем в нем триггер валидации
        var triggerService = serviceProvider.GetRequiredService<DatabaseTriggerService>();
        triggerService.Register<IDomainObjectHasKey<Guid>, FluentValidationTrigger>();

        // Настройка опций БД с подключением нашего интерцептора
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DatabaseTriggerInterceptor(triggerService, serviceProvider))
            .Options;

        await using var context = new IntegrationDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        // Создаем невалидный объект (пустое имя)
        var entity = new IntegrationEntity { Name = "" };
        context.Add(entity);

        // --- ACT & ASSERT ---

        // Проверяем, что вся цепочка сработала и выбросила OperationCanceledException
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        // Проверяем, что сообщение об ошибке пришло именно от нашего валидатора
        Assert.Equal("NameRequired", exception.Message);
    }
}