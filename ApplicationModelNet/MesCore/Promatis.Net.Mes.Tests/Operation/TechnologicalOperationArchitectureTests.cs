using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Tests.Operation;

public class TechnologicalOperationArchitectureTests
{
    private readonly TestOperationValidator _validator = new();
    private readonly DbContextOptions<OperationTestDbContext> _options;

    public TechnologicalOperationArchitectureTests()
    {
        _options = new DbContextOptionsBuilder<OperationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static EntityCancelEventArgs<IDomainObjectHasKey<Guid>> CreateArgs(IDomainObjectHasKey<Guid> entity, DbContext context)
    {
        return new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            entity,
            EntityStateChangeEnum.Added,
            new List<PropertyChangeInfo>(),
            context);
    }

    #region Валидация (FluentValidation)

    [Fact]
    public void Validator_Should_Allow_Links_When_Operation_Is_Leaf()
    {
        // Arrange
        var operation = new TestOperation { IsLeaf = true, Name = "Конечная операция" };
        operation.UnitLinks.Add(new TestOperationUnit { Id = Guid.NewGuid() });

        // Act
        TestValidationResult<TestOperation>? result = _validator.TestValidate(operation);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_Should_Reject_Links_When_Operation_Is_Group()
    {
        // Arrange
        var operation = new TestOperation { IsLeaf = false, Name = "Группа операций" };
        operation.UnitLinks.Add(new TestOperationUnit { Id = Guid.NewGuid() });

        // Act
        TestValidationResult<TestOperation>? result = _validator.TestValidate(operation);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UnitLinks)
              .WithErrorMessage("Группа операций не может содержать прямые связи с оборудованием.");
    }

    #endregion

    #region Триггер защиты дерева (ReferenceTreeParentTrigger)

    [Fact]
    public async Task Trigger_Should_Allow_Valid_Operation_Hierarchy()
    {
        // Arrange
        await using var context = new OperationTestDbContext(_options);
        var parent = new TestOperation { Id = Guid.NewGuid(), Name = "Родительская группа", IsLeaf = false };
        context.Operations.Add(parent);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new TestOperation { Name = "Дочерняя операция", ParentId = parent.Id };
        var trigger = new ReferenceTreeParentTrigger();
        EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args = CreateArgs(child, context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        args.Cancel.Should().BeFalse();
        args.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Trigger_Should_Cancel_When_Operation_Is_Its_Own_Parent()
    {
        // Arrange
        await using var context = new OperationTestDbContext(_options);
        var operationId = Guid.NewGuid();
        var operation = new TestOperation { Id = operationId, ParentId = operationId, Name = "Петля" };

        var trigger = new ReferenceTreeParentTrigger();
        EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args = CreateArgs(operation, context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        args.Cancel.Should().BeTrue();
        args.ErrorMessage.Should().Be("Объект не может быть родителем самому себе.");
    }

    [Fact]
    public async Task Trigger_Should_Cancel_When_Deep_Cyclic_Dependency_Detected_In_Operations()
    {
        // Arrange
        await using var context = new OperationTestDbContext(_options);

        var opA = new TestOperation { Id = Guid.NewGuid(), Name = "Операция А" };
        context.Operations.Add(opA);

        var opB = new TestOperation { Id = Guid.NewGuid(), Name = "Операция Б", ParentId = opA.Id };
        context.Operations.Add(opB);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Пытаемся замкнуть цикл: А делаем сыном Б (Цепочка: Б -> А -> Б)
        opA.ParentId = opB.Id;

        var trigger = new ReferenceTreeParentTrigger();
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            opA, EntityStateChangeEnum.Modified, new List<PropertyChangeInfo>(), context);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        args.Cancel.Should().BeTrue();
        args.ErrorMessage.Should().Contain("Циклическая зависимость");
    }

    #endregion

    #region Тестирование связей Many-to-Many (Operation <-> Unit)

    [Fact]
    public async Task OperationUnitLink_Should_Successfully_Persist_With_Priority()
    {
        // Arrange
        await using var context = new OperationTestDbContext(_options);

        var operation = new TestOperation { Id = Guid.NewGuid(), Name = "Токарная операция" };
        var unit = new TestUnitNode
        {
            Id = Guid.NewGuid(),
            Name = "Станок 01",
            Type = UnitType.Table
        };

        context.Operations.Add(operation);
        context.Units.Add(unit);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var link = new TestOperationUnit
        {
            Id = Guid.NewGuid(),
            OperationId = operation.Id,
            UnitId = unit.Id,
            Priority = 10
        };
        context.OperationUnits.Add(link);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        TechnologicalOperationUnitBase? persistedLink = await context.OperationUnits.FirstOrDefaultAsync(x => x.Id == link.Id, cancellationToken: TestContext.Current.CancellationToken);
        persistedLink.Should().NotBeNull();
        persistedLink!.Priority.Should().Be(10);
        persistedLink.OperationId.Should().Be(operation.Id);
        persistedLink.UnitId.Should().Be(unit.Id);
    }

    #endregion
}