using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Archive;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Archive;

public sealed class ArchiveKnowledgeNodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithOwnedActiveNode_ShouldArchiveNode()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var repository = new FakeRepository(node);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new ArchiveKnowledgeNodeCommand(node.Id),
            CancellationToken.None);

        node.Status.Should().Be(KnowledgeNodeStatus.Archived);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithAlreadyArchivedNode_ShouldBeNoOp()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        node.Archive(DateTimeOffset.UtcNow);
        var repository = new FakeRepository(node);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new ArchiveKnowledgeNodeCommand(node.Id),
            CancellationToken.None);

        node.Status.Should().Be(KnowledgeNodeStatus.Archived);
        repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WithMissingNode_ShouldThrowNotFoundException()
    {
        var handler = CreateHandler(new FakeRepository(), Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new ArchiveKnowledgeNodeCommand(Guid.NewGuid()),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersNode_ShouldThrowNotFoundException()
    {
        var node = CreateNode(Guid.NewGuid());
        var handler = CreateHandler(
            new FakeRepository(node),
            Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new ArchiveKnowledgeNodeCommand(node.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_NOT_FOUND");
    }

    private static ArchiveKnowledgeNodeHandler CreateHandler(
        FakeRepository repository,
        Guid userId)
    {
        return new ArchiveKnowledgeNodeHandler(
            repository,
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId
            });
    }

    private static KnowledgeNode CreateNode(Guid userId)
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            null,
            "Node",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }
    }

    private sealed class FakeRepository(KnowledgeNode? node = null)
        : IKnowledgeNodeRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(node?.Id == id ? node : null);
        }

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeNode>>([]);
        }

        public void Add(KnowledgeNode knowledgeNode)
        {
            throw new NotSupportedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
