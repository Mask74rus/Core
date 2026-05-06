using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.Tests;

public class UnitServiceStandaloneTests
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    // Вспомогательный класс для тестов, так как UnitBase абстрактный
    public class TestUnit : Domain.UnitBase { }

    public UnitServiceStandaloneTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Исправленная настройка: используем Task.FromResult и приведение к базовому контексту
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<ApplicationDbContext>(new TestDbContext(_options)));

        _factory = factoryMock.Object;
    }

    // Тестовый контекст, который "знает" про TestUnit
    private class TestDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options, new ConfigurationBuilder().Build())
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Не вызываем base.OnModelCreating(modelBuilder), если там сложная логика с Configuration
            // Либо убедитесь, что конвенции не падают из-за null конфигурации.

            modelBuilder.Entity<Domain.UnitBase>().UseTptMappingStrategy();
            modelBuilder.Entity<TestUnit>().ToTable("TestUnits");
        }
    }

    [Fact]
    public async Task GetByKindAsync_Should_Return_Correct_Units()
    {
        // Arrange
        var service = new UnitService(_factory);
        var unit1 = new TestUnit { Id = Guid.NewGuid(), Name = "S1", Kind = UnitKind.Storage, Type = UnitType.Storage };
        var unit2 = new TestUnit { Id = Guid.NewGuid(), Name = "P1", Kind = UnitKind.Production, Type = UnitType.Workshop };

        await service.AddAsync(unit1);
        await service.AddAsync(unit2);

        // Act
        var result = await service.GetByKindAsync(UnitKind.Storage);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("S1");
    }

    [Fact]
    public async Task GetByTypeAsync_Should_Handle_Flags_Correctly()
    {
        // Arrange
        var service = new UnitService(_factory);
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Machine", Kind = UnitKind.Production, Type = UnitType.MachineTool });
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Table", Kind = UnitKind.Production, Type = UnitType.Table });

        // Act
        var result = await service.GetByTypeAsync(UnitType.Table);

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Table");
    }

    [Fact]
    public async Task GetFullTreeAsync_Should_Work_For_Units()
    {
        // Arrange
        var service = new UnitService(_factory);
        var rootId = Guid.NewGuid();
        var root = new TestUnit { Id = rootId, Name = "MainWarehouse", Kind = UnitKind.Storage, Type = UnitType.Storage };
        var child = new TestUnit { Id = Guid.NewGuid(), Name = "ZoneA", Kind = UnitKind.Storage, Type = UnitType.Zone, ParentId = rootId };

        await service.AddAsync(root);
        await service.AddAsync(child);

        // Act
        var tree = await service.GetFullTreeAsync(rootId);

        // Assert
        tree.Should().NotBeNull();
        tree!.Children.Should().HaveCount(1);
        tree.Children.First().Name.Should().Be("ZoneA");
    }
}