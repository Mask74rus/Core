using FluentValidation.TestHelper;
using Promatis.Net.Domain;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Validator;

public class ReferenceTreeBaseValidatorTests
{
    // Тестовая сущность для проверки абстрактного класса
    private class TestNode : ReferenceTreeBase { }

    // Тестовый валидатор
    private class TestNodeValidator : ReferenceTreeBaseValidator<TestNode> { }

    private readonly TestNodeValidator _validator = new();

    [Fact]
    public void Validator_ShouldFail_WhenParentIdIsEqualToId()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new TestNode
        {
            Id = nodeId,
            ParentId = nodeId, // Ошибка: ID совпадает с ParentId
            Name = "Self-referencing node"
        };

        // Act
        TestValidationResult<TestNode>? result = _validator.TestValidate(node);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentId)
              .WithErrorMessage("Объект не может быть родителем самому себе");
    }

    [Fact]
    public void Validator_ShouldFail_WhenParentIdIsEmptyGuid()
    {
        // Arrange
        var node = new TestNode
        {
            ParentId = Guid.Empty, // Ошибка по второму правилу
            Name = "Empty ParentId"
        };

        // Act
        TestValidationResult<TestNode>? result = _validator.TestValidate(node);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParentId)
              .WithErrorMessage("Родительский идентификатор не может быть пустым GUID");
    }

    [Fact]
    public void Validator_ShouldPass_WhenParentIdIsDifferent()
    {
        // Arrange
        var node = new TestNode
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.NewGuid(),
            Name = "Valid child node"
        };

        // Act
        TestValidationResult<TestNode>? result = _validator.TestValidate(node);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ParentId);
    }

    [Fact]
    public void Validator_ShouldPass_WhenParentIdIsNull()
    {
        // Arrange (Корень дерева)
        var node = new TestNode
        {
            Id = Guid.NewGuid(),
            ParentId = null,
            Name = "Root node"
        };

        // Act
        TestValidationResult<TestNode>? result = _validator.TestValidate(node);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ParentId);
    }
}