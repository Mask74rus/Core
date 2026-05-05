using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promatis.Net.Configuration;
using Promatis.Net.Data;
using Promatis.Net.Domain;

namespace Promatis.Net.ApplicationModel.Console;

public class ConsoleConfigurator : AppConfigurator
{
    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Вызываем базовую логику (регистрация триггеров, инфраструктуры)
        base.ConfigureServices(services, configuration);

        // Специфика консоли: Провайдер пользователя
        services.AddScoped<IUserProvider, ConsoleUserProvider>();

        // Специфика консоли: БД (например, InMemory для тестов)
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("ConsoleDb")
                .AddInterceptors(sp.GetRequiredService<DatabaseTriggerInterceptor>());
        });
    }

    public override void ConfigureApp(IHost app)
    {
        // Сначала запускаем базовую активацию триггеров
        base.ConfigureApp(app);

        // Тут можно добавить тестовый запуск
        _ = RunDemo(app);
    }

    private async Task RunDemo(IHost app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        await using var context = await factory.CreateDbContextAsync();
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("--- КРИТИЧЕСКАЯ ОШИБКА ПРИ СОЗДАНИИ БД ---");
            System.Console.WriteLine(ex.Message);
            if (ex.InnerException != null)
            {
                System.Console.WriteLine("Детали: " + ex.InnerException.Message);
            }
            throw; // Чтобы увидеть StackTrace
        }

        System.Console.WriteLine("\n--- ТЕСТ 1: Валидация ---");
        try
        {
            // Создаем категорию с нарушением правил (если они есть)
            var category = new Category { Name = "" }; // Допустим, NameRequired
            context.Add(category);
            await context.SaveChangesAsync();
        }
        catch (OperationCanceledException ex)
        {
            System.Console.WriteLine($"[SUCCESS] Валидация сработала: {ex.Message}");
        }

        System.Console.WriteLine("\n--- ТЕСТ 2: Дерево (Self-Parenting) ---");
        try
        {
            var node = new Category { Name = "SelfParent" };
            await context.SaveChangesAsync(); // Сначала сохраним, чтобы получить Id

            node.ParentId = node.Id;
            context.Update(node);
            await context.SaveChangesAsync();
        }
        catch (OperationCanceledException ex)
        {
            System.Console.WriteLine($"[SUCCESS] Триггер дерева сработал: {ex.Message}");
        }
    }
}