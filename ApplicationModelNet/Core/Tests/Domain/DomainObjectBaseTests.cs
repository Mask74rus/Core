using Moq;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class DomainObjectBaseTests
{
    // Создаем минимальную реализацию для тестов
    private class TestDomainObject : DomainObjectBase<int> { }

    [Fact]
    public void DomainObject_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var domainObject = new TestDomainObject();

        // Assert
        Assert.Equal(default, domainObject.Id);
        // Проверяем, что дата создания установилась корректно (с допуском в 1 секунду)
        Assert.True((DateTime.UtcNow - domainObject.CreatedAt).TotalSeconds < 1);
    }

    [Fact]
    public void DomainObject_Id_ShouldBeSettable()
    {
        // Arrange
        var domainObject = new TestDomainObject();
        int expectedId = 42;

        // Act
        domainObject.Id = expectedId;

        // Assert
        Assert.Equal(expectedId, domainObject.Id);
    }

    [Fact]
    public void Interface_ShouldAllowMocking()
    {
        // Пример использования Moq для интерфейса (если он будет передаваться в сервисы)
        var mock = new Mock<IDomainObjectHasKey<string>>();
        mock.SetupProperty(m => m.Id, "test-guid");

        Assert.Equal("test-guid", mock.Object.Id);
    }
}