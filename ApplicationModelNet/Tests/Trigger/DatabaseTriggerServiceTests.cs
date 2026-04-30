using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class DatabaseTriggerServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly DatabaseTriggerService _service;

    public DatabaseTriggerServiceTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);

        _service = new DatabaseTriggerService(_scopeFactoryMock.Object);
    }

    private class TestEntity : DomainObject { }

    [Fact]
    public async Task ValidateAsync_ShouldExecuteRegisteredBeforeSaveAction()
    {
        // Arrange
        bool wasCalled = false;
        _service.BeforeSave<TestEntity>(args =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Assert
        Assert.True(wasCalled);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowException_WhenTriggerCancels()
    {
        // Arrange
        var errorMessage = "Stop right there!";
        _service.BeforeSave<TestEntity>(args =>
        {
            args.Cancel = true;
            args.ErrorMessage = errorMessage;
            return Task.CompletedTask;
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!));

        Assert.Equal(errorMessage, ex.Message);
    }

    [Fact]
    public async Task Hierarchy_ShouldInvokeTriggerForInterface()
    {
        // Arrange: Регистрируем триггер на интерфейс, который реализует TestEntity
        bool interfaceTriggerCalled = false;
        _service.BeforeSave<IDomainObject>(args =>
        {
            interfaceTriggerCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Assert
        Assert.True(interfaceTriggerCalled, "Триггер для интерфейса должен быть вызван для реализующего его класса");
    }

    
    private class InvalidTrigger { }

    [Fact]
    public void Register_ShouldThrow_WhenTriggerDoesNotImplementInterfaces()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _service.Register<TestEntity, InvalidTrigger>());
    }

    [Fact]
    public async Task NotifyAsync_ShouldStopExecution_WhenHandledIsTrue()
    {
        // Arrange
        int callCount = 0;
        _service.AfterSave<TestEntity>(args => { callCount++; args.Handled = true; return Task.CompletedTask; });
        _service.AfterSave<TestEntity>(args => { callCount++; return Task.CompletedTask; });

        // Act
        await _service.NotifyAsync(new TestEntity(), EntityStateChangeEnum.Added, [], "User", DateTime.UtcNow);

        // Assert
        Assert.Equal(1, callCount); // Второй триггер не должен вызваться
    }
}