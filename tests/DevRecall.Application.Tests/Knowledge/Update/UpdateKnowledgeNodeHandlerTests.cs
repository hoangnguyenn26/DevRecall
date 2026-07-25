using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Update;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Update;

public sealed class UpdateKnowledgeNodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithOwnedActiveNode_ShouldRenameNode()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var repository = new FakeRepository(node);
        var handler = CreateHandler(repository, userId);

        var result = await handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(node.Id, "  Software Engineering  "),
            CancellationToken.None);

        result.Title.Should().Be("Software Engineering");
        node.Title.Should().Be("Software Engineering");
        repository.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithMissingNode_ShouldThrowNotFoundException()
    {
        var handler = CreateHandler(new FakeRepository(), Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(Guid.NewGuid(), "Updated"),
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
            new UpdateKnowledgeNodeCommand(node.Id, "Updated"),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithArchivedNode_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        node.Archive(DateTimeOffset.UtcNow);
        var handler = CreateHandler(new FakeRepository(node), userId);

        var action = () => handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(node.Id, "Updated"),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_NODE_ARCHIVED");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsync_WithEmptyTitle_ShouldThrowValidationException(
        string title)
    {
        var handler = CreateHandler(new FakeRepository(), Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(Guid.NewGuid(), title),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors["title"].Should()
            .ContainSingle("Title is required.");
    }

    [Fact]
    public async Task HandleAsync_WithLongTitle_ShouldThrowValidationException()
    {
        var handler = CreateHandler(new FakeRepository(), Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(
                Guid.NewGuid(),
                new string('a', 201)),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors["title"].Should()
            .ContainSingle("Title cannot exceed 200 characters.");
    }

    private static UpdateKnowledgeNodeHandler CreateHandler(
        FakeRepository repository,
        Guid userId)
    {
        return new UpdateKnowledgeNodeHandler(
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
            "Programming",
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
        public bool SaveChangesCalled { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(node?.Id == id ? node : null);
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
            return Task.FromResult<IReadOnlyList<KnowledgeNode>>([]);
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

        public void Add(KnowledgeNode knowledgeNode)
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
