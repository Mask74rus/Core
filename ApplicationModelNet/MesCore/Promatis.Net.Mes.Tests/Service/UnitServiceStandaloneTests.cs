using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.Tests;



public class UnitServiceStandaloneTests
{
    // 1. Используем везде TestDbContext
    private readonly IDbContextFactory<TestDbContext> _factory;
    private readonly DbContextOptions<TestDbContext> _options;

    public class TestUnit : Domain.UnitBase { }

    public class TestUnitService(IDbContextFactory<TestDbContext> factory)
        : UnitBaseService<TestDbContext>(factory)
    { }

    public UnitServiceStandaloneTests()
    {
        // 2. Опции должны быть для TestDbContext
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // 3. Мокаем фабрику именно для TestDbContext
        var factoryMock = new Mock<IDbContextFactory<TestDbContext>>();

        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TestDbContext(_options)); // ReturnsAsync упрощает Task.FromResult

        _factory = factoryMock.Object;
    }

    // 4. Контекст тоже должен принимать правильные опции
    public class TestDbContext(DbContextOptions<TestDbContext> options)
        : ApplicationDbContext(options, new ConfigurationBuilder().Build())
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Domain.UnitBase>().UseTptMappingStrategy();
            modelBuilder.Entity<TestUnit>().ToTable("TestUnits");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            IEnumerable<EntityEntry<Domain.UnitBase>> entries = ChangeTracker.Entries<Domain.UnitBase>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified);

            foreach (EntityEntry<Domain.UnitBase> entry in entries)
            {
                Domain.UnitBase unit = entry.Entity;
                if (unit.ParentId.HasValue)
                {
                    // Загружаем родителя для проверки (в InMemory Find работает быстро)
                    Domain.UnitBase? parent = Set<Domain.UnitBase>().Find(unit.ParentId.Value);
                    if (parent != null)
                    {
                        // 1. Правило Position (Терминальный узел)
                        if (parent.Kind == UnitKind.Position)
                            throw new InvalidOperationException("Position node cannot have children.");

                        // 2. Правило Department (Не может содержать Position)
                        if (parent.Kind == UnitKind.Department && unit.Kind == UnitKind.Position)
                            throw new InvalidOperationException($"Нарушение иерархии: объект категории '{unit.Kind}' не может быть вложен в '{parent.Kind}'.");

                        // 3. Правило изоляции (Production/Storage/Transport не смешиваются)
                        if (parent.Kind is UnitKind.Production or UnitKind.Storage or UnitKind.Transport)
                        {
                            if (unit.Kind != parent.Kind && unit.Kind != UnitKind.Position)
                                throw new InvalidOperationException($"Нарушение иерархии: '{unit.Kind}' не может быть вложен в '{parent.Kind}'.");
                        }
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }



    [Fact]
    public async Task GetByKindAsync_Should_Return_Units_Matching_Complex_Mask()
    {
        // Arrange
        var service = new TestUnitService(_factory);

        // Ячейка входит в маску Storage
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Cell_01", Kind = UnitKind.Storage, Type = UnitType.Cell });
        // Станок входит в маску Production
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Lathe_01", Kind = UnitKind.Production, Type = UnitType.MachineTool });

        // Act
        List<Domain.UnitBase> storageUnits = await service.GetByKindAsync(UnitKind.Storage);

        // Assert
        storageUnits.Should().ContainSingle(x => x.Name == "Cell_01");
        storageUnits.Should().NotContain(x => x.Name == "Lathe_01");
    }

    [Fact]
    public async Task GetByTypeAsync_Should_Handle_Specific_Flags()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Crane_1", Kind = UnitKind.Storage, Type = UnitType.Crane });
        await service.AddAsync(new TestUnit { Id = Guid.NewGuid(), Name = "Vehicle_1", Kind = UnitKind.Transport, Type = UnitType.Vehicle });

        // Act
        List<Domain.UnitBase> result = await service.GetByTypeAsync(UnitType.Crane);

        // Assert
        result.Should().ContainSingle(x => x.Name == "Crane_1");
    }

    [Fact]
    public async Task MoveAsync_Should_Throw_When_Moving_Into_Position()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        var posId = Guid.NewGuid();
        var machineId = Guid.NewGuid();

        await service.AddAsync(new TestUnit { Id = posId, Name = "Terminal Point", Kind = UnitKind.Position, Type = UnitType.Other });
        await service.AddAsync(new TestUnit { Id = machineId, Name = "Mobile Machine", Kind = UnitKind.Production, Type = UnitType.MachineTool });

        // Act & Assert
        Func<Task> act = async () => await service.MoveAsync(machineId, posId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Position node cannot have children.");
    }

    [Fact]
    public async Task MoveAsync_Should_Prevent_Circular_Dependencies()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        // Добавляем Kind и Type, так как они обязательны (required / init)
        await service.AddAsync(new TestUnit
        {
            Id = idA,
            Name = "Node A",
            Kind = UnitKind.Department,
            Type = UnitType.Workshop
        });

        await service.AddAsync(new TestUnit
        {
            Id = idB,
            Name = "Node B",
            ParentId = idA,
            Kind = UnitKind.Production,
            Type = UnitType.Section
        });

        // Act & Assert
        Func<Task> act = async () => await service.MoveAsync(idA, idB);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Циклическая зависимость*");
    }

    [Fact]
    public async Task GetFullTreeAsync_Should_Maintain_Hierarchy_And_References()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        var rootId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();

        await service.AddAsync(new TestUnit
        {
            Id = rootId,
            Name = "Dept",
            Kind = UnitKind.Department,
            Type = UnitType.Workshop
        });

        await service.AddAsync(new TestUnit
        {
            Id = sectionId,
            Name = "Section",
            Kind = UnitKind.Production,
            Type = UnitType.Section,
            ParentId = rootId
        });

        await service.AddAsync(new TestUnit
        {
            Id = Guid.NewGuid(),
            Name = "Workstation",
            Kind = UnitKind.Production,
            Type = UnitType.Workstation,
            ParentId = sectionId
        });

        // Act
        Domain.UnitBase? tree = await service.GetFullTreeAsync(rootId);

        // Assert
        tree.Should().NotBeNull();
        tree!.Children.Should().HaveCount(1);
        tree.Children.First().Children.Should().HaveCount(1);
    }

    [Fact]
    public async Task MoveAsync_Department_Should_Not_Contain_Position()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        var deptId = Guid.NewGuid();
        var positionId = Guid.NewGuid();

        // 1. Создаем Департамент
        await service.AddAsync(new TestUnit
        {
            Id = deptId,
            Name = "Main Dept",
            Kind = UnitKind.Department,
            Type = UnitType.Workshop
        });

        // 2. Создаем Позицию (согласно твоей маске, это может быть Cell)
        await service.AddAsync(new TestUnit
        {
            Id = positionId,
            Name = "Work Point 1",
            Kind = UnitKind.Position,
            Type = UnitType.Cell
        });

        // Act
        // Пытаемся переместить Position в Department
        Func<Task> act = async () => await service.MoveAsync(positionId, deptId);

        // Assert
        // Ожидаем ошибку от UnitBaseHierarchyTrigger
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*категории 'Position' не может быть вложен в 'Department'*");
    }

    [Fact]
    public async Task MoveAsync_Production_Should_Contain_Production_Subnode()
    {
        // Arrange
        var service = new TestUnitService(_factory);
        var parentProdId = Guid.NewGuid();
        var childProdId = Guid.NewGuid();

        await service.AddAsync(new TestUnit
        {
            Id = parentProdId,
            Name = "Production Area",
            Kind = UnitKind.Production,
            Type = UnitType.Workshop
        });

        await service.AddAsync(new TestUnit
        {
            Id = childProdId,
            Name = "Machine Node",
            Kind = UnitKind.Production,
            Type = UnitType.MachineTool
        });

        // Act
        Func<Task> act = async () => await service.MoveAsync(childProdId, parentProdId);

        // Assert
        // Здесь исключения быть не должно, так как Production может содержать Production
        await act.Should().NotThrowAsync();

        Domain.UnitBase? updated = await service.GetByIdAsync(childProdId);
        updated!.ParentId.Should().Be(parentProdId);
    }
}