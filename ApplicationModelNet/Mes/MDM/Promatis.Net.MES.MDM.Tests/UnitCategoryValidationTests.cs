using FluentAssertions;
using FluentValidation.TestHelper;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.MDM.Domain;

namespace Promatis.Net.MES.MDM.Tests;


// Реализации сущностей
public class TestDept : DepartmentUnitBase { }
public class TestProd : ProductionUnitBase { }
public class TestStor : StorageUnitBase { }
public class TestPos : PositionUnitBase { }
public class TestTrans : TransportUnitBase { }

// Реализации валидаторов
public class TestDeptValidator : DepartmentUnitBaseValidator<TestDept> { }
public class TestProdValidator : ProductionUnitBaseValidator<TestProd> { }
public class TestStorValidator : StorageUnitBaseValidator<TestStor> { }
public class TestPosValidator : PositionUnitBaseValidator<TestPos> { }
public class TestTransValidator : TransportUnitBaseValidator<TestTrans> { }

public class UnitCategoryValidationTests
{
    private readonly TestDeptValidator _deptValidator = new();
    private readonly TestProdValidator _prodValidator = new();
    private readonly TestStorValidator _storValidator = new();
    private readonly TestPosValidator _posValidator = new();
    private readonly TestTransValidator _transValidator = new();

    [Theory]
    [InlineData(UnitType.Workshop, true)]  // Ок для Департамента
    [InlineData(UnitType.Section, true)]   // Ок для Департамента
    [InlineData(UnitType.Crane, false)]    // Ошибка: Кран не может быть Департаментом
    public void DepartmentValidator_Should_Verify_Types(UnitType type, bool isValid)
    {
        var unit = new TestDept { Type = type, Name = "Dept" };
        TestValidationResult<TestDept>? result = _deptValidator.TestValidate(unit);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Тип не соответствует категории.");
    }

    [Theory]
    [InlineData(UnitType.MachineTool, true)] // Ок для Production
    [InlineData(UnitType.Table, true)]       // Ок для Production
    [InlineData(UnitType.Storage, false)]     // Ошибка: Склад не может быть Production
    public void ProductionValidator_Should_Verify_Types(UnitType type, bool isValid)
    {
        var unit = new TestProd { Type = type, Name = "Prod" };
        TestValidationResult<TestProd>? result = _prodValidator.TestValidate(unit);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Тип не соответствует категории.");
    }

    [Theory]
    [InlineData(UnitType.Cell, true)]    // Ячейка — это Ок для Склада
    [InlineData(UnitType.Rack, true)]    // Стеллаж — это Ок для Склада
    [InlineData(UnitType.Vehicle, false)] // Транспорт — не Склад
    public void StorageValidator_Should_Verify_Types(UnitType type, bool isValid)
    {
        var unit = new TestStor { Type = type, Name = "Stor" };
        TestValidationResult<TestStor>? result = _storValidator.TestValidate(unit);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Тип не соответствует категории.");
    }

    [Theory]
    [InlineData(UnitType.Cell, true)]    // Ячейка может быть Позицией
    [InlineData(UnitType.Other, true)]   // Other может быть Позицией
    [InlineData(UnitType.Workshop, false)] // Цех не может быть Позицией
    public void PositionValidator_Should_Verify_Types(UnitType type, bool isValid)
    {
        var unit = new TestPos { Type = type, Name = "Pos" };
        TestValidationResult<TestPos>? result = _posValidator.TestValidate(unit);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Тип не соответствует категории.");
    }

    [Theory]
    [InlineData(UnitType.Vehicle, true)]   // Погрузчик — Ок для Транспорта
    [InlineData(UnitType.Conveyor, true)]  // Конвейер — Ок для Транспорта
    [InlineData(UnitType.MachineTool, false)] // Станок — не Транспорт
    [InlineData(UnitType.Cell, false)]     // Ячейка — не Транспорт
    public void TransportValidator_Should_Verify_Types(UnitType type, bool isValid)
    {
        var unit = new TestTrans { Type = type, Name = "Trans" };
        TestValidationResult<TestTrans>? result = _transValidator.TestValidate(unit);

        if (isValid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Тип не соответствует категории.");
    }


    [Fact]
    public void Kind_Should_Be_Automatically_Set_By_Base_Class()
    {
        // Проверяем, что наши новые абстрактные классы сами проставляют Kind.
        // Передаем обязательный Type и Name для соблюдения ограничений required.

        new TestDept { Type = UnitType.Other, Name = "Test" }.Kind.Should().Be(UnitKind.Department);
        new TestProd { Type = UnitType.Other, Name = "Test" }.Kind.Should().Be(UnitKind.Production);
        new TestStor { Type = UnitType.Other, Name = "Test" }.Kind.Should().Be(UnitKind.Storage);
        new TestPos { Type = UnitType.Other, Name = "Test" }.Kind.Should().Be(UnitKind.Position);
        new TestTrans { Type = UnitType.Other, Name = "Test" }.Kind.Should().Be(UnitKind.Transport);
    }
}