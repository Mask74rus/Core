using FluentValidation;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class FluentValidationTriggerTests
{
    private class TestEntity : DomainObject { public string Name { get; set; } = ""; }

    private class TestEntityValidator : AbstractValidator<TestEntity>
    {
        public TestEntityValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public FluentValidationTriggerTests()
    {
        // Теперь нам нужен только провайдер
        _serviceProviderMock = new Mock<IServiceProvider>();
    }

    [Fact]
    public async Task HandleAsync_ShouldCancel_WhenValidationFails()
    {
        // Arrange
        var entity = new TestEntity { Name = "" };
        var validator = new TestEntityValidator();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestEntity>)))
            .Returns(validator);

        // Внедряем провайдер напрямую в триггер
        var trigger = new FluentValidationTrigger(_serviceProviderMock.Object);
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            entity, EntityStateChangeEnum.Added, [], null!);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Contains("Name is required", args.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCancel_WhenValidationSucceeds()
    {
        // Arrange
        var entity = new TestEntity { Name = "Valid Name" };
        var validator = new TestEntityValidator();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestEntity>)))
            .Returns(validator);

        var trigger = new FluentValidationTrigger(_serviceProviderMock.Object);
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            entity, EntityStateChangeEnum.Added, [], null!);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.False(args.Cancel);
        Assert.Null(args.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ShouldIgnore_WhenNoValidatorRegistered()
    {
        // Arrange
        _serviceProviderMock
            .Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(null!);

        var trigger = new FluentValidationTrigger(_serviceProviderMock.Object);
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.False(args.Cancel);
    }
}