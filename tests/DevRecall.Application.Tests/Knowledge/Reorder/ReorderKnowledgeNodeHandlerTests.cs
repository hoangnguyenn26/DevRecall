using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Reorder;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.Reorder;

public sealed class ReorderKnowledgeNodeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAlreadyAtTargetIndex_ShouldNotWrite()
    {
        var userId = Guid.NewGuid();
        var node = KnowledgeNode.Create(
            Guid.NewGuid(), userId, null, "Programming", 0, DateTimeOffset.UtcNow);
        var repository = new FakeRepository(node);
        var handler = new ReorderKnowledgeNodeHandler(repository, new FakeCurrentUser(userId));

        await handler.HandleAsync(
            new ReorderKnowledgeNodeCommand(node.Id, 0),
            CancellationToken.None);

        repository.SaveChangesCalled.Should().BeFalse();
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(KnowledgeNode node) : IKnowledgeNodeRepository
    {
        public bool SaveChangesCalled { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<KnowledgeNode?>(node.Id == id ? node : null);

        public Task<KnowledgeNode?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<KnowledgeNode?>(
                node.Id == id && node.UserId == userId ? node : null);

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNode>>([node]);

        public Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNodeHierarchyItem>>([]);

        public Task<int> GetNextSortOrderAsync(
            Guid userId, Guid? parentId, CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveSiblingsAsync(
            Guid userId, Guid? parentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNode>>([node]);

        public void Add(KnowledgeNode knowledgeNode) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
