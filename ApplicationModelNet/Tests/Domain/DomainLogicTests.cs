using Promatis.Net.Domain;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class DomainLogicTests
{
    // Реализации для тестирования абстрактных классов
    private class TestDomainObject : DomainObject { }
    private class TestReference : ReferenceBase { }

    [Fact]
    public void DomainObject_EveryInstance_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var obj1 = new TestDomainObject();
        var obj2 = new TestDomainObject();
        var obj3 = new TestDomainObject();

        // Assert
        Assert.NotEqual(obj1.Id, obj2.Id);
        Assert.NotEqual(obj2.Id, obj3.Id);
        Assert.NotEqual(Guid.Empty, obj1.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ReferenceBase_Name_CanBeNullOrEmpty_TechnicalCheck(string? invalidName)
    {
        // Arrange
        var reference = new TestReference();

        // Act
        reference.Name = invalidName!; // Используем !, так как Name в коде не nullable

        // Assert
        // Пока мы просто проверяем, что свойство принимает эти значения
        // Если вы добавите валидацию в будущем, этот тест поможет её отладить
        Assert.Equal(invalidName, reference.Name);
    }

    [Fact]
    public void ReferenceBase_ShouldHoldValidName()
    {
        // Arrange
        var reference = new TestReference();
        var expectedName = "Основной склад";

        // Act
        reference.Name = expectedName;

        // Assert
        Assert.Equal(expectedName, reference.Name);
    }
}