using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Service;

public class BaseServiceTests_Crud : BaseServiceTests
{
    // Тестовая сущность
    public class TestEntity : DomainObject { public string Title { get; set; } = ""; }

    // Реализация сервиса для теста с указанием контекста
    public class TestService(IDbContextFactory<ApplicationDbContext> f)
        : BaseService<TestEntity, Guid, ApplicationDbContext>(f);

    [Fact]
    public async Task AddAsync_Should_SaveEntityToDatabase()
    {
        // Arrange
        var service = new TestService(Factory);
        var entity = new TestEntity { Id = Guid.NewGuid(), Title = "Hello" };

        // Act
        await service.AddAsync(entity);

        // Assert
        TestEntity? saved = await service.GetByIdAsync(entity.Id);
        Assert.NotNull(saved);
        Assert.Equal("Hello", saved.Title);
    }

    [Fact]
    public async Task UpdateAsync_Should_ModifyExistingEntity()
    {
        // Arrange
        var service = new TestService(Factory);
        var entity = new TestEntity { Id = Guid.NewGuid(), Title = "Old" };
        await service.AddAsync(entity);

        // Act
        entity.Title = "New";
        await service.UpdateAsync(entity);

        // Assert
        TestEntity? updated = await service.GetByIdAsync(entity.Id);
        Assert.Equal("New", updated?.Title);
    }

    [Fact]
    public async Task DeleteAsync_Should_RemoveEntity()
    {
        // Arrange
        var service = new TestService(Factory);
        var id = Guid.NewGuid();
        await service.AddAsync(new TestEntity { Id = id });

        // Act
        await service.DeleteAsync(id);

        // Assert
        TestEntity? deleted = await service.GetByIdAsync(id);
        Assert.Null(deleted);
    }

}
