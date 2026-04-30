using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class ReferenceBaseTests
{
    private class TestReference : ReferenceBase { }

    [Fact]
    public void ReferenceObject_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var reference = new TestReference();

        // Assert
        Assert.Equal(string.Empty, reference.Name); // Проверяем string.Empty
        Assert.Null(reference.Description);
        Assert.Null(reference.Code);
        Assert.Null(reference.DeletedAt); // Изначально не удалено
        Assert.Null(reference.DeletedBy);

        // Проверяем, что ID всё еще генерируется (наследование от DomainObject)
        Assert.NotEqual(Guid.Empty, reference.Id);
    }

    [Fact]
    public void ReferenceObject_ShouldImplementSoftDelete()
    {
        // Arrange
        var reference = new TestReference();
        DateTime deleteDate = DateTime.UtcNow;
        string adminName = "Admin";

        // Act
        // Приводим к интерфейсу, чтобы убедиться в корректности реализации
        ISoftDeletable softDelete = reference;
        softDelete.DeletedAt = deleteDate;
        softDelete.DeletedBy = adminName;

        // Assert
        Assert.Equal(deleteDate, reference.DeletedAt);
        Assert.Equal(adminName, reference.DeletedBy);
    }

    [Fact]
    public void ReferenceObject_Properties_ShouldBeSettable()
    {
        // Arrange
        var reference = new TestReference();
        string expectedName = "Справочник";
        string expectedCode = "REF_001";

        // Act
        reference.Name = expectedName;
        reference.Code = expectedCode;

        // Assert
        Assert.Equal(expectedName, reference.Name);
        Assert.Equal(expectedCode, reference.Code);
    }
}
