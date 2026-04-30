using FluentValidation.TestHelper;
using Promatis.Net.Domain;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Validator;

public class DomainObjectValidatorTests
{
    // 1. Создаем тестовый объект и его валидатор
    private class TestEntity : DomainObject { }

    private class TestEntityValidator : DomainObjectValidator<TestEntity> { }

    private readonly TestEntityValidator _validator = new();

    [Fact]
    public void Validator_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.Empty };

        // Act
        TestValidationResult<TestEntity>? result = _validator.TestValidate(entity);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Идентификатор объекта не может быть пустым.");
    }

    [Fact]
    public void Validator_ShouldFail_WhenCreatedAtIsInFuture()
    {
        // Arrange
        var entity = new TestEntity
        {
            CreatedAt = DateTime.UtcNow.AddMinutes(5)
        };

        // Act
        TestValidationResult<TestEntity>? result = _validator.TestValidate(entity);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatedAt)
            .WithErrorMessage("Дата создания не может быть в будущем.");
    }

    [Fact]
    public void Validator_ShouldPass_WhenObjectIsValid()
    {
        // Arrange
        var entity = new TestEntity(); // Id и CreatedAt заполняются в конструкторе базовых классов

        // Act
        TestValidationResult<TestEntity>? result = _validator.TestValidate(entity);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}