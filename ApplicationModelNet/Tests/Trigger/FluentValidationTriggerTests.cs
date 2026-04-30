using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Trigger;

public class FluentValidationTriggerTests
{
    private class TestEntity : DomainObject { public string Name { get; set; } = ""; }

    // Простой валидатор для теста
    private class TestEntityValidator : AbstractValidator<TestEntity>
    {
        public TestEntityValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly DatabaseTriggerService _triggerService;

    public FluentValidationTriggerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        scopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);

        _triggerService = new DatabaseTriggerService(scopeFactoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancel_WhenValidationFails()
    {
        // Arrange
        var entity = new TestEntity { Name = "" }; // Вызовет ошибку
        var validator = new TestEntityValidator();

        // Регистрируем валидатор в моке провайдера
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IValidator<TestEntity>)))
            .Returns(validator);

        var trigger = new FluentValidationTrigger(_triggerService);
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            entity, EntityStateChangeEnum.Added, [], null!);

        // Act
        await trigger.HandleAsync(args);

        // Assert
        Assert.True(args.Cancel);
        Assert.Equal("Name is required", args.ErrorMessage);
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

        var trigger = new FluentValidationTrigger(_triggerService);
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
        // Arrange: провайдер вернет null для валидатора
        _serviceProviderMock
            .Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(null!);

        var trigger = new FluentValidationTrigger(_triggerService);
        var args = new EntityCancelEventArgs<IDomainObjectHasKey<Guid>>(
            new TestEntity(), EntityStateChangeEnum.Added, [], null!);

        // Act
        await trigger.HandleAsync(args);

        // Assert: триггер не должен падать или отменять сохранение
        Assert.False(args.Cancel);
    }
}