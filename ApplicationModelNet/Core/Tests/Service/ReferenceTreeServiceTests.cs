using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.Service;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Service;


public class ReferenceTreeServiceTests : BaseServiceTests
{
    /// <summary>
    /// Рабочая тестовая сущность для верификации иерархических алгоритмов.
    /// ИСПРАВЛЕНО: Базовый класс ReferenceTreeBase<T> закрыт типом TestTreeEntity, дублирование свойств удалено.
    /// </summary>
    public class TestTreeEntity : ReferenceTreeBase<TestTreeEntity>
    {
        // Свойства Parent и Children автоматически унаследованы из ReferenceTreeBase<TestTreeEntity> 
        // и идеально закрывают контракт ITreeNode<TestTreeEntity> под капотом!
    }

    /// <summary>
    /// Рабочий тестовый сервис.
    /// ИСПРАВЛЕНО: Реализует абстрактный хук платформы CreateChildTemplateAsync, 
    /// необходимый для компиляции базового ReferenceTreeService.
    /// </summary>
    public class TestTreeService(IDbContextFactory<ApplicationDbContext> f)
        : ReferenceTreeService<TestTreeEntity, ApplicationDbContext>(f)
    {
        /// <summary>
        /// Простейшая тестовая фабрика создания пустых шаблонов узлов для прогона иерархических тестов.
        /// </summary>
        public override Task<TestTreeEntity> CreateChildTemplateAsync(TestTreeEntity parent)
        {
            var child = new TestTreeEntity
            {
                ParentId = parent.Id
            };
            child.Parent = null;
            return Task.FromResult(child);
        }
    }

    [Fact]
    public async Task GetParentPathAsync_Should_Return_Correct_Order()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var rootId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await service.AddAsync(new TestTreeEntity { Id = rootId, Name = "Root" });
        await service.AddAsync(new TestTreeEntity { Id = parentId, Name = "Parent", ParentId = rootId });
        await service.AddAsync(new TestTreeEntity { Id = childId, Name = "Child", ParentId = parentId });

        // Act
        List<TestTreeEntity> path = await service.GetParentPathAsync(childId);

        // Assert
        Assert.Equal(3, path.Count);
        Assert.Equal("Root", path[0].Name);
        Assert.Equal("Parent", path[1].Name);
        Assert.Equal("Child", path[2].Name);
    }

    [Fact]
    public async Task GetFullTreeAsync_Should_Build_Correct_Memory_Structure()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var rootId = Guid.NewGuid();

        await service.AddAsync(new TestTreeEntity { Id = rootId, Name = "Root" });
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "Child1", ParentId = rootId });
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "Child2", ParentId = rootId });

        // Act
        TestTreeEntity? tree = await service.GetFullTreeAsync(rootId);

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(2, tree.Children.Count);
        // Проверка обратной связи (Parent)
        Assert.All(tree.Children, child => Assert.Same(tree, child.Parent));
    }

    [Fact]
    public async Task GetRootsAsync_Should_Return_Only_Entities_Without_Parent()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var rootId = Guid.NewGuid();

        // 1. Создаем честный корень
        await service.AddAsync(new TestTreeEntity { Id = rootId, Name = "Root" });

        // 2. Создаем ребенка, привязанного к существующему корню
        // Это гарантирует, что дерево валидно
        await service.AddAsync(new TestTreeEntity
        {
            Id = Guid.NewGuid(),
            Name = "Child",
            ParentId = rootId // Используем реальный ID
        });

        // Act
        List<TestTreeEntity> roots = await service.GetRootsAsync();

        // Assert
        // Должен остаться только один корень
        Assert.Single(roots);
        Assert.Null(roots[0].ParentId);
        Assert.Equal("Root", roots[0].Name);
    }

    [Fact]
    public async Task GetChildrenAsync_Should_Return_Direct_Descendants()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var parentId = Guid.NewGuid();
        await service.AddAsync(new TestTreeEntity { Id = parentId, Name = "Parent" });
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "C1", ParentId = parentId });
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "C2", ParentId = parentId });

        // Act
        List<TestTreeEntity> children = await service.GetChildrenAsync(parentId);

        // Assert
        Assert.Equal(2, children.Count);
        Assert.All(children, c => Assert.Equal(parentId, c.ParentId));
    }

    [Fact]
    public async Task MoveAsync_Should_Update_ParentId_Successfully()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var oldParentId = Guid.NewGuid();
        var newParentId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        await service.AddAsync(new TestTreeEntity { Id = oldParentId, Name = "Old Parent" });
        await service.AddAsync(new TestTreeEntity { Id = newParentId, Name = "New Parent" });
        await service.AddAsync(new TestTreeEntity { Id = targetId, Name = "Target", ParentId = oldParentId });

        // Act
        await service.MoveAsync(targetId, newParentId, TestContext.Current.CancellationToken);

        // Assert
        TestTreeEntity? updated = await service.GetByIdAsync(targetId);
        Assert.Equal(newParentId, updated!.ParentId);
    }

    [Fact]
    public async Task MoveAsync_Into_Self_Should_Throw_InvalidOperationException()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var targetId = Guid.NewGuid();
        await service.AddAsync(new TestTreeEntity { Id = targetId, Name = "Self" });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MoveAsync(targetId, targetId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MoveAsync_Into_Own_Subtree_Should_Throw_InvalidOperationException()
    {
        // Arrange (Создаем иерархию: A -> B -> C)
        var service = new TestTreeService(Factory);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        await service.AddAsync(new TestTreeEntity { Id = idA, Name = "A" });
        await service.AddAsync(new TestTreeEntity { Id = idB, Name = "B", ParentId = idA });
        await service.AddAsync(new TestTreeEntity { Id = idC, Name = "C", ParentId = idB });

        // Act & Assert 
        // Пытаемся переместить A внутрь своего внука C
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MoveAsync(idA, idC, TestContext.Current.CancellationToken));

        Assert.Contains("Циклическая зависимость", exception.Message);
    }

    [Fact]
    public async Task MoveAsync_To_Null_Should_Move_To_Root()
    {
        // Arrange
        var service = new TestTreeService(Factory);
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        await service.AddAsync(new TestTreeEntity { Id = parentId, Name = "Parent" });
        await service.AddAsync(new TestTreeEntity { Id = childId, Name = "Child", ParentId = parentId });

        // Act
        await service.MoveAsync(childId, null, TestContext.Current.CancellationToken);

        // Assert
        TestTreeEntity? updated = await service.GetByIdAsync(childId);
        Assert.Null(updated!.ParentId);
    }
}