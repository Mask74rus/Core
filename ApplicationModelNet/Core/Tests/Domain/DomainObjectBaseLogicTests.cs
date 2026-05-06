using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class DomainObjectBaseLogicTests
{
    private class ConcreteDomainObject : DomainObject { }

    [Fact]
    public void DomainObject_ShouldGenerateNewGuidInConstructor()
    {
        // Arrange & Act
        var obj1 = new ConcreteDomainObject();
        var obj2 = new ConcreteDomainObject();

        // Assert
        Assert.NotEqual(Guid.Empty, obj1.Id);
        Assert.NotEqual(Guid.Empty, obj2.Id);
        Assert.NotEqual(obj1.Id, obj2.Id);
    }

    [Fact]
    public void DomainObject_ShouldCastToIDomainObjectAndHaveCreatedAt()
    {
        // Arrange
        var obj = new ConcreteDomainObject();

        // Act
        IDomainObject baseInterface = obj;

        // Assert
        // Теперь мы только проверяем наличие значения. 
        // Попытка записи baseInterface.CreatedAt = newDate здесь не скомпилируется.
        Assert.True(baseInterface.CreatedAt > DateTime.MinValue);
        Assert.True((DateTime.UtcNow - baseInterface.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void CreatedAt_ShouldAllowManualInitialization_OnCreation()
    {
        // Arrange
        var customDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        // Работает только через инициализатор объекта при создании
        var obj = new ConcreteDomainObject { CreatedAt = customDate };

        // Assert
        Assert.Equal(customDate, obj.CreatedAt);
    }
}