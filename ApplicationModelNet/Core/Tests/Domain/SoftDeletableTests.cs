using Moq;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Domain;

public class SoftDeletableTests
{
    [Fact]
    public void ISoftDeletable_Mock_ShouldStoreValues()
    {
        // Arrange
        var mock = new Mock<ISoftDeletable>();
        DateTime deleteDate = DateTime.UtcNow;
        string deletedBy = "SystemAdmin";

        // Настраиваем свойства (в Moq для интерфейсов нужно SetupProperty)
        mock.SetupProperty(m => m.DeletedAt);
        mock.SetupProperty(m => m.DeletedBy);

        // Act
        ISoftDeletable obj = mock.Object;
        obj.DeletedAt = deleteDate;
        obj.DeletedBy = deletedBy;

        // Assert
        Assert.Equal(deleteDate, obj.DeletedAt);
        Assert.Equal(deletedBy, obj.DeletedBy);
    }

    [Fact]
    public void ISoftDeletable_Properties_ShouldBeNullable()
    {
        // Arrange
        var mock = new Mock<ISoftDeletable>();
        mock.SetupProperty(m => m.DeletedAt);
        mock.SetupProperty(m => m.DeletedBy);

        ISoftDeletable obj = mock.Object;

        // Act
        obj.DeletedAt = null;
        obj.DeletedBy = null;

        // Assert
        Assert.Null(obj.DeletedAt);
        Assert.Null(obj.DeletedBy);
    }

    [Fact]
    public void ReferenceBase_ShouldCorrectlyCastToISoftDeletable()
    {
        // Проверка того, что ваша иерархия классов действительно поддерживает контракт
        // Arrange
        var referenceObject = new TestReference(); // класс из предыдущего теста

        // Act
        bool isSoftDeletable = referenceObject is ISoftDeletable;

        // Assert
        Assert.True(isSoftDeletable, "ReferenceBase должен реализовывать ISoftDeletable");
    }

    // Вспомогательный класс для теста приведения типов
    private class TestReference : ReferenceBase { }
}