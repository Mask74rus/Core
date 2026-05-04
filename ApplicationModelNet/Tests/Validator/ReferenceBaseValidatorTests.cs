using FluentValidation.TestHelper;
using Promatis.Net.Domain;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Validator;

public class ReferenceBaseValidatorTests
{
    // Тестовая сущность и её валидатор
    private class TestReference : ReferenceBase { }
    private class TestReferenceValidator : ReferenceBaseValidator<TestReference> { }

    private readonly TestReferenceValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] // Меньше 2 символов
    public void Name_ShouldHaveErrors_WhenInvalid(string? invalidName)
    {
        var model = new TestReference { Name = invalidName! };
        TestValidationResult<TestReference>? result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_ShouldHaveError_WhenTooLong()
    {
        var model = new TestReference { Name = new string('A', 251) };
        TestValidationResult<TestReference>? result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Превышена максимальная длина наименования (250 символов)");
    }

    [Theory]
    [InlineData("VALID_CODE_123")]
    [InlineData("REF1")]
    [InlineData("")] // Теперь это пройдет успешно
    [InlineData(null)] // И это тоже
    public void Code_ShouldBeValid_WhenCorrectFormat(string? code)
    {
        var model = new TestReference { Code = code };
        TestValidationResult<TestReference>? result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("код_на_русском")]
    [InlineData("lower_case")]
    [InlineData("CODE-1")] // Тире запрещено
    public void Code_ShouldHaveError_WhenInvalidFormat(string invalidCode)
    {
        var model = new TestReference { Code = invalidCode };
        TestValidationResult<TestReference>? result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public async Task ValidateValue_ShouldReturnErrors_OnlyForSpecificProperty()
    {
        // Arrange
        var model = new TestReference
        {
            Name = "", // Ошибка
            Code = "INVALID-CODE" // Ошибка
        };

        // Act
        // Проверяем работу вашего делегата ValidateValue для поля Name
        IEnumerable<string> nameErrors = await _validator.ValidateValue(model, nameof(TestReference.Name));
        IEnumerable<string> codeErrors = await _validator.ValidateValue(model, nameof(TestReference.Code));

        // Assert
        Assert.Contains(nameErrors, e => e.Contains("Наименование обязательно"));
        Assert.Contains(codeErrors, e => e.Contains("Код может содержать только латиницу"));
    }

    [Theory]
    [InlineData(" Name")]
    [InlineData("Name ")]
    public void Name_ShouldHaveError_WhenHasLeadingOrTrailingSpaces(string invalidName)
    {
        var model = new TestReference { Name = invalidName };
        TestValidationResult<TestReference>? result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Наименование не может начинаться или заканчиваться пробелами");
    }

    [Fact]
    public async Task ValidateValue_ShouldIgnoreOtherPropertiesErrors()
    {
        // Arrange: оба поля невалидны
        var model = new TestReference { Name = "A", Code = "low_case" };

        // Act: валидируем ТОЛЬКО Name
        IEnumerable<string> nameErrors = await _validator.ValidateValue(model, nameof(TestReference.Name));

        // Assert
        Assert.Single(nameErrors); // Только ошибка длины имени
        Assert.DoesNotContain(nameErrors, e => e.Contains("Код")); // Ошибок кода быть не должно
    }
}