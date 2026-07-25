using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.UpdateContent;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.UpdateContent;

public sealed class UpdateKnowledgeContentHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithStaleTimestamp_ShouldRejectWithoutSaving()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        var repository = new FakeRepository(node);
        var handler = new UpdateKnowledgeContentHandler(repository, new FakeCurrentUser(userId));

        var action = () => handler.HandleAsync(
            new UpdateKnowledgeContentCommand(
                node.Id,
                "Stale content",
                node.UpdatedAtUtc.AddMinutes(-1)),
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ConflictException>();
        exception.Which.ErrorCode.Should().Be("KNOWLEDGE_CONCURRENT_UPDATE");
        repository.SaveChangesCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithEquivalentContent_ShouldPreserveTimestampWithoutSaving()
    {
        var userId = Guid.NewGuid();
        var node = CreateNode(userId);
        node.UpdateContent("Current content", node.UpdatedAtUtc.AddMinutes(1));
        var timestamp = node.UpdatedAtUtc;
        var repository = new FakeRepository(node);
        var handler = new UpdateKnowledgeContentHandler(repository, new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new UpdateKnowledgeContentCommand(node.Id, "  Current content  ", timestamp),
            CancellationToken.None);

        result.UpdatedAtUtc.Should().Be(timestamp);
        repository.SaveChangesCalled.Should().BeFalse();
    }

    private static KnowledgeNode CreateNode(Guid userId)
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            null,
            "Dictionary",
            0,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(KnowledgeNode node) : IKnowledgeNodeRepository
    {
        public bool SaveChangesCalled { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<KnowledgeNode?>(node.Id == id ? node : null);
        }

        public Task<KnowledgeNode?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<KnowledgeNode?>(
                node.Id == id && node.UserId == userId ? node : null);
        }

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
            Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeNode>>([]);
        }

        public Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
            Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeNodeHierarchyItem>>([]);
        }

        public Task<int> GetNextSortOrderAsync(
            Guid userId, Guid? parentId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveSiblingsAsync(
            Guid userId, Guid? parentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNode>>([]);

        public Task<IReadOnlyList<KnowledgeNodeWithTagsItem>> GetActiveByTagIdsAsync(
            Guid userId, IReadOnlyCollection<Guid> tagIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNodeWithTagsItem>>([]);

        public void Add(KnowledgeNode knowledgeNode) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
