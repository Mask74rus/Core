using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.MES.Data;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Tests.UnitBase;

public class UnitBaseArchitectureTests
{
    private readonly TestUnitValidator _validator = new();
    private readonly DbContextOptions<AppDbContext> _options;

    public UnitBaseArchitectureTests()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static EntityCancelEventArgs<MES.Domain.UnitBase> CreateArgs(MES.Domain.UnitBase entity, DbContext context)
    {
        return new EntityCancelEventArgs<MES.Domain.UnitBase>(
            entity,
            EntityStateChangeEnum.Added,
            new List<PropertyChangeInfo>(),
            context);
    }

    #region Валидация (Маски и базовые правила)

    [Theory]
    [InlineData(UnitKind.Storage, UnitType.Cell, true)]      // Ячейка в Складе — Ок
    [InlineData(UnitKind.Storage, UnitType.Workshop, false)] // Цех в Складе — Ошибка (бит не совпадает)
    [InlineData(UnitKind.Production, UnitType.Table, true)] // Стол в Производстве — Ок
    [InlineData(UnitKind.Position, UnitType.Other, true)]   // Прочее в Позиции — Ок
    [InlineData(UnitKind.Position, UnitType.Table, false)]  // Стол в Позиции — Ошибка (согласно финальному enum)
    public void Validator_Should_Check_Bitwise_Compatibility(UnitKind kind, UnitType type, bool isValid)
    {
        var unit = new TestUnit { Kind = kind, Type = type, Name = "Test" };
        TestValidationResult<TestUnit>? result = _validator.TestValidate(unit);

        if (isValid)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("Тип не соответствует категории.");
        }
    }

    #endregion

    #region Триггер (Правила вложенности Kind)

    [Fact]
    public async Task Trigger_Should_Allow_Root_Units()
    {
        await using var context = new AppDbContext(_options);
        var root = new TestUnit { ParentId = null, Kind = UnitKind.Department, Type = UnitType.Other };

        var trigger = new UnitBaseHierarchyTrigger();
        EntityCancelEventArgs<Domain.UnitBase> args = CreateArgs(root, context);

        await trigger.HandleAsync(args);

        args.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Trigger_Should_Reject_Position_In_Department()
    {
        await using var context = new AppDbContext(_options);
        var parentId = Guid.NewGuid();

        context.Units.Add(new TestUnit { Id = parentId, Kind = UnitKind.Department, Type = UnitType.Other, Name = "Dept" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new TestUnit { ParentId = parentId, Kind = UnitKind.Position, Type = UnitType.Other, Name = "Pos" };
        var trigger = new UnitBaseHierarchyTrigger();
        EntityCancelEventArgs<Domain.UnitBase> args = CreateArgs(child, context);

        await trigger.HandleAsync(args);

        args.Cancel.Should().BeTrue();
        args.ErrorMessage.Should().Contain("не может быть вложен в 'Dept' (Department)");
    }

    [Theory]
    [InlineData(UnitKind.Production, UnitKind.Production, false)] // Цех в Цех — Ок
    [InlineData(UnitKind.Production, UnitKind.Transport, true)]   // Транспорт в Цех — Запрещено (разные Kind)
    [InlineData(UnitKind.Storage, UnitKind.Position, false)]      // Позиция в Склад — Ок
    [InlineData(UnitKind.Position, UnitKind.Position, true)]      // Позиция в Позицию — Запрещено (терминальный узел)
    public async Task Trigger_Should_Enforce_Isolation(UnitKind pKind, UnitKind cKind, bool shouldCancel)
    {
        await using var context = new AppDbContext(_options);
        var parentId = Guid.NewGuid();

        // Создаем родителя с подходящим типом
        context.Units.Add(new TestUnit { Id = parentId, Kind = pKind, Type = GetFirstType(pKind), Name = "P" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new TestUnit { ParentId = parentId, Kind = cKind, Type = GetFirstType(cKind), Name = "C" };
        EntityCancelEventArgs<Domain.UnitBase> args = CreateArgs(child, context);
        await new UnitBaseHierarchyTrigger().HandleAsync(args);

        args.Cancel.Should().Be(shouldCancel, $"потому что вложение {cKind} в {pKind} должно привести к Cancel = {shouldCancel}");
    }

    #endregion

    private static UnitType GetFirstType(UnitKind kind)
        => Enum.GetValues<UnitType>().First(t => t != UnitType.None && ((int)kind & (int)t) != 0);
}