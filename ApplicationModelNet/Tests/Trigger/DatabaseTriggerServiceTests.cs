using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;


// --- ТЕСТОВЫЕ КЛАССЫ (теперь достаточно одного набора) ---
public class TestEntity : DomainObject, IAudit { }
public interface ITestBeforeTrigger : IBeforeSaveTrigger<TestEntity> { }
public interface ITestAfterTrigger : IAfterSaveTrigger<TestEntity> { }
public interface ISecondAfterTrigger : IAfterSaveTrigger<TestEntity> { }
public class InvalidTrigger { }

public class DatabaseTriggerServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly DatabaseTriggerService _service;

    public DatabaseTriggerServiceTests()
    {
        // 1. Очищаем статику перед каждым тестом
        DatabaseTriggerService.ClearInternalRegistrations();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _service = new DatabaseTriggerService(_serviceProviderMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_ShouldExecuteRegisteredTrigger()
    {
        // Arrange
        var triggerMock = new Mock<ITestBeforeTrigger>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestBeforeTrigger)))
                            .Returns(triggerMock.Object);

        _service.Register<TestEntity, ITestBeforeTrigger>();

        // Act
        await _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Assert
        triggerMock.Verify(x => x.HandleAsync(It.IsAny<EntityCancelEventArgs<TestEntity>>()), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowException_WhenTriggerCancels()
    {
        // Arrange
        string errorMessage = "Stop!";
        var triggerMock = new Mock<ITestBeforeTrigger>();
        triggerMock.Setup(x => x.HandleAsync(It.IsAny<EntityCancelEventArgs<TestEntity>>()))
                   .Callback<EntityCancelEventArgs<TestEntity>>(args =>
                   {
                       args.Cancel = true;
                       args.ErrorMessage = errorMessage;
                   });

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestBeforeTrigger)))
                            .Returns(triggerMock.Object);

        _service.Register<TestEntity, ITestBeforeTrigger>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!));

        Assert.Equal(errorMessage, ex.Message);
    }

    [Fact]
    public async Task Hierarchy_ShouldInvokeTriggerForInterface()
    {
        // Arrange
        var triggerMock = new Mock<IBeforeSaveTrigger<IAudit>>();
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBeforeSaveTrigger<IAudit>)))
                            .Returns(triggerMock.Object);

        // Регистрируем на интерфейс
        _service.Register<IAudit, IBeforeSaveTrigger<IAudit>>();

        // Act: TestEntity реализует IAudit
        await _service.ValidateAsync(new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Assert
        triggerMock.Verify(x => x.HandleAsync(It.IsAny<EntityCancelEventArgs<IAudit>>()), Times.Once);
    }

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
        var triggerMock1 = new Mock<ITestAfterTrigger>();
        triggerMock1.Setup(x => x.HandleAsync(It.IsAny<EntityChangedEventArgs<TestEntity>>()))
                    .Callback<EntityChangedEventArgs<TestEntity>>(args => args.Handled = true);

        var triggerMock2 = new Mock<ISecondAfterTrigger>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestAfterTrigger))).Returns(triggerMock1.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(ISecondAfterTrigger))).Returns(triggerMock2.Object);

        _service.Register<TestEntity, ITestAfterTrigger>();
        _service.Register<TestEntity, ISecondAfterTrigger>();

        // Act
        await _service.NotifyAsync(new TestEntity(), EntityStateChangeEnum.Added, [], "User", DateTime.UtcNow);

        // Assert
        triggerMock1.Verify(x => x.HandleAsync(It.IsAny<EntityChangedEventArgs<TestEntity>>()), Times.Once);
        triggerMock2.Verify(x => x.HandleAsync(It.IsAny<EntityChangedEventArgs<TestEntity>>()), Times.Never);
    }
}