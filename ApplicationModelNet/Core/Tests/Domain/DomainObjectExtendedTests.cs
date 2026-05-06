using Promatis.Net.Domain;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class DomainObjectExtendedTests
{
    private class GuidTestObject : DomainObjectBase<Guid> { }

    [Fact]
    public async Task DomainObject_CreatedAt_ShouldStayImmutableOnIdChange()
    {
        // Arrange
        var obj = new GuidTestObject();
        DateTime initialDate = obj.CreatedAt;
        var newId = Guid.NewGuid();

        // Небольшая пауза, чтобы убедиться, что время не «тикает» в свойстве
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // Act
        obj.Id = newId;

        // Assert
        Assert.Equal(newId, obj.Id);
        Assert.Equal(initialDate, obj.CreatedAt); // Дата не должна измениться
    }
}