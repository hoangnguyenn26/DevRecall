using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Create;

public sealed class CreateKnowledgeNodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithRootCommand_ShouldCreateOwnedRootNode()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeKnowledgeNodeRepository();
        var handler = CreateHandler(repository, userId);

        var result = await handler.HandleAsync(
            new CreateKnowledgeNodeCommand("  Programming  ", null),
            CancellationToken.None);

        repository.AddedNode.Should().NotBeNull();
        repository.AddedNode!.UserId.Should().Be(userId);
        repository.AddedNode.ParentId.Should().BeNull();
        repository.AddedNode.Title.Should().Be("Programming");
        repository.SaveChangesCalled.Should().BeTrue();
        result.Id.Should().Be(repository.AddedNode.Id);
    }

    [Fact]
    public async Task HandleAsync_WithValidParent_ShouldCreateChildNode()
    {
        var userId = Guid.NewGuid();
        var parent = CreateNode(userId);
        var repository = new FakeKnowledgeNodeRepository(parent);
        var handler = CreateHandler(repository, userId);

        var result = await handler.HandleAsync(
            new CreateKnowledgeNodeCommand("C#", parent.Id),
            CancellationToken.None);

        result.ParentId.Should().Be(parent.Id);
        repository.AddedNode!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task HandleAsync_WithMissingParent_ShouldThrowNotFoundException()
    {
        var handler = CreateHandler(
            new FakeKnowledgeNodeRepository(),
            Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand("Child", Guid.NewGuid()),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithAnotherUsersParent_ShouldThrowNotFoundException()
    {
        var parent = CreateNode(Guid.NewGuid());
        var handler = CreateHandler(
            new FakeKnowledgeNodeRepository(parent),
            Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand("Child", parent.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NotFoundException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_PARENT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_WithArchivedParent_ShouldThrowConflictException()
    {
        var userId = Guid.NewGuid();
        var parent = CreateNode(userId);
        parent.Archive(DateTimeOffset.UtcNow);
        var handler = CreateHandler(
            new FakeKnowledgeNodeRepository(parent),
            userId);

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand("Child", parent.Id),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_INVALID_PARENT");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task HandleAsync_WithEmptyTitle_ShouldThrowValidationException(
        string title)
    {
        var handler = CreateHandler(
            new FakeKnowledgeNodeRepository(),
            Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand(title, null),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors["title"].Should()
            .ContainSingle("Title is required.");
    }

    [Fact]
    public async Task HandleAsync_WithLongTitle_ShouldThrowValidationException()
    {
        var handler = CreateHandler(
            new FakeKnowledgeNodeRepository(),
            Guid.NewGuid());

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand(new string('a', 201), null),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors["title"].Should()
            .ContainSingle("Title cannot exceed 200 characters.");
    }

    [Fact]
    public async Task HandleAsync_WithoutAuthenticatedUser_ShouldThrowUnauthorizedException()
    {
        var handler = new CreateKnowledgeNodeHandler(
            new FakeKnowledgeNodeRepository(),
            new FakeCurrentUser());

        var action = () => handler.HandleAsync(
            new CreateKnowledgeNodeCommand("Node", null),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.ErrorCode.Should().Be("IDENTITY_UNAUTHENTICATED");
    }

    private static CreateKnowledgeNodeHandler CreateHandler(
        FakeKnowledgeNodeRepository repository,
        Guid userId)
    {
        return new CreateKnowledgeNodeHandler(
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
            "Parent",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }

        public Guid? UserId { get; init; }
    }

    private sealed class FakeKnowledgeNodeRepository(
        KnowledgeNode? existingNode = null)
        : IKnowledgeNodeRepository
    {
        public KnowledgeNode? AddedNode { get; private set; }

        public bool SaveChangesCalled { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                existingNode?.Id == id ? existingNode : null);
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

        public void Add(KnowledgeNode node)
        {
            AddedNode = node;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
