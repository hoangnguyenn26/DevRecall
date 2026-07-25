using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.GetTree;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.GetTree;

public sealed class GetKnowledgeTreeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNoNodes_ShouldReturnEmptyTree()
    {
        var userId = Guid.NewGuid();
        var handler = CreateHandler(userId, []);

        var tree = await handler.HandleAsync(CancellationToken.None);

        tree.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithHierarchy_ShouldBuildNestedTree()
    {
        var userId = Guid.NewGuid();
        var root = CreateNode(userId, title: "A");
        var firstChild = CreateNode(userId, root.Id, "B");
        var secondChild = CreateNode(userId, root.Id, "C");
        var grandchild = CreateNode(userId, firstChild.Id, "D");
        var handler = CreateHandler(
            userId,
            [grandchild, secondChild, root, firstChild]);

        var tree = await handler.HandleAsync(CancellationToken.None);

        tree.Should().ContainSingle();
        tree[0].Title.Should().Be("A");
        tree[0].Children.Select(item => item.Title)
            .Should().Equal("B", "C");
        tree[0].Children[0].Children.Should().ContainSingle();
        tree[0].Children[0].Children[0].Title.Should().Be("D");
        tree[0].Children[0].Children[0].Children.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldRequestOnlyCurrentUsersNodes()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeKnowledgeNodeRepository([]);
        var handler = new GetKnowledgeTreeHandler(
            repository,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId
            });

        await handler.HandleAsync(CancellationToken.None);

        repository.RequestedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderRootsAndChildrenByTitle()
    {
        var userId = Guid.NewGuid();
        var zebra = CreateNode(userId, title: "Zebra");
        var apple = CreateNode(userId, title: "Apple");
        var database = CreateNode(userId, title: "Database");
        var zChild = CreateNode(userId, apple.Id, "Z child");
        var aChild = CreateNode(userId, apple.Id, "A child");
        var handler = CreateHandler(
            userId,
            [zebra, zChild, database, aChild, apple]);

        var tree = await handler.HandleAsync(CancellationToken.None);

        tree.Select(item => item.Title)
            .Should().Equal("Apple", "Database", "Zebra");
        tree[0].Children.Select(item => item.Title)
            .Should().Equal("A child", "Z child");
    }

    [Fact]
    public async Task HandleAsync_WithOrphanedActiveChild_ShouldNotPromoteChildToRoot()
    {
        var userId = Guid.NewGuid();
        var child = CreateNode(userId, Guid.NewGuid(), "Child");
        var handler = CreateHandler(userId, [child]);

        var tree = await handler.HandleAsync(CancellationToken.None);

        tree.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var handler = new GetKnowledgeTreeHandler(
            new FakeKnowledgeNodeRepository([]),
            new FakeCurrentUser());

        var action = () => handler.HandleAsync(CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_UNAUTHENTICATED");
    }

    private static GetKnowledgeTreeHandler CreateHandler(
        Guid userId,
        IReadOnlyList<KnowledgeNode> nodes)
    {
        return new GetKnowledgeTreeHandler(
            new FakeKnowledgeNodeRepository(nodes),
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId
            });
    }

    private static KnowledgeNode CreateNode(
        Guid userId,
        Guid? parentId = null,
        string title = "Node")
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            parentId,
            title,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }
    }

    private sealed class FakeKnowledgeNodeRepository(
        IReadOnlyList<KnowledgeNode> nodes)
        : IKnowledgeNodeRepository
    {
        public Guid? RequestedUserId { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(nodes.SingleOrDefault(node => node.Id == id));
        }

        public async Task<KnowledgeNode?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken)
        {
            var node = await GetByIdAsync(id, cancellationToken);
            return node?.UserId == userId ? node : null;
        }

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            return Task.FromResult(nodes);
        }

        public Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeNodeHierarchyItem>>([]);
        }

        public Task<int> GetNextSortOrderAsync(
            Guid userId, Guid? parentId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public void Add(KnowledgeNode node)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
