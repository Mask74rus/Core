using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Xunit;

namespace Promatis.Net.ApplicationModel.Tests.Service;

public class ReferenceTreeServiceTests : BaseServiceTests
{
    public class TestTreeEntity : ReferenceTreeBase<TestTreeEntity> { }

    public class TestTreeService(IDbContextFactory<ApplicationDbContext> f)
        : ReferenceTreeService<TestTreeEntity>(f);

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
        var path = await service.GetParentPathAsync(childId);

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
        var tree = await service.GetFullTreeAsync(rootId);

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
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "Root" });
        await service.AddAsync(new TestTreeEntity { Id = Guid.NewGuid(), Name = "Child", ParentId = Guid.NewGuid() });

        // Act
        var roots = await service.GetRootsAsync();

        // Assert
        Assert.Single(roots);
        Assert.Null(roots[0].ParentId);
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
        var children = await service.GetChildrenAsync(parentId);

        // Assert
        Assert.Equal(2, children.Count);
        Assert.All(children, c => Assert.Equal(parentId, c.ParentId));
    }
}