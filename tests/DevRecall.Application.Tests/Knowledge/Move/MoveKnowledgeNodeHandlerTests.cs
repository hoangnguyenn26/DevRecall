using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Move;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Move;

public sealed class MoveKnowledgeNodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidParent_ShouldMoveNode()
    {
        var userId = Guid.NewGuid();
        var firstParent = CreateNode(userId);
        var secondParent = CreateNode(userId);
        var node = CreateNode(userId, firstParent.Id);
        var repository = new FakeRepository(
            firstParent,
            secondParent,
            node);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, secondParent.Id),
            CancellationToken.None);

        node.ParentId.Should().Be(secondParent.Id);
        repository.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithNullParent_ShouldMoveNodeToRoot()
    {
        var userId = Guid.NewGuid();
        var parent = CreateNode(userId);
        var node = CreateNode(userId, parent.Id);
        var repository = new FakeRepository(parent, node);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, null),
            CancellationToken.None);

        node.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithMissingNode_ShouldThrowNotFoundException()
    {
        var handler = CreateHandler(new FakeRepository(), Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(Guid.NewGuid(), null),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithMissingParent_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var handler = CreateHandler(new FakeRepository(node), userId);

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, Guid.NewGuid()),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersParent_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var parent = CreateNode(Guid.NewGuid());
        var handler = CreateHandler(
            new FakeRepository(node, parent),
            userId);

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, parent.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithArchivedParent_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var parent = CreateNode(userId);
        parent.Archive(DateTimeOffset.UtcNow);
        var handler = CreateHandler(
            new FakeRepository(node, parent),
            userId);

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, parent.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_INVALID_PARENT");
    }

    [Fact]
    public async Task HandleAsync_WithArchivedNode_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        node.Archive(DateTimeOffset.UtcNow);
        var handler = CreateHandler(new FakeRepository(node), userId);

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(node.Id, null),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_ARCHIVED");
    }

    [Fact]
    public async Task HandleAsync_WithDeepCycle_ShouldThrowWithoutMutatingNode()
    {
        var userId = Guid.NewGuid();
        var root = CreateNode(userId);
        var child = CreateNode(userId, root.Id);
        var grandchild = CreateNode(userId, child.Id);
        var repository = new FakeRepository(root, child, grandchild);
        var handler = CreateHandler(repository, userId);

        var action = () => handler.HandleAsync(
            new MoveKnowledgeNodeCommand(root.Id, grandchild.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should()
            .Be("KNOWLEDGE_CIRCULAR_HIERARCHY");
        root.ParentId.Should().BeNull();
        repository.SaveChangesCalled.Should().BeFalse();
    }

    private static MoveKnowledgeNodeHandler CreateHandler(
        FakeRepository repository,
        Guid userId)
    {
        return new MoveKnowledgeNodeHandler(
            repository,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId
            });
    }

    private static KnowledgeNode CreateNode(
        Guid userId,
        Guid? parentId = null)
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            parentId,
            "Node",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }
    }

    private sealed class FakeRepository(params KnowledgeNode[] nodes)
        : IKnowledgeNodeRepository
    {
        public bool SaveChangesCalled { get; private set; }

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
            return Task.FromResult<IReadOnlyList<KnowledgeNode>>(
                nodes.Where(node => node.UserId == userId).ToList());
        }

        public Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<KnowledgeNodeHierarchyItem> hierarchy = nodes
                .Where(node => node.UserId == userId)
                .Select(node => new KnowledgeNodeHierarchyItem(
                    node.Id,
                    node.ParentId))
                .ToList();

            return Task.FromResult(hierarchy);
        }

        public void Add(KnowledgeNode node)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
