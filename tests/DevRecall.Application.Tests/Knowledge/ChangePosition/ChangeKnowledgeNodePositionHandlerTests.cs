using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.ChangePosition;
using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge.ChangePosition;

public sealed class ChangeKnowledgeNodePositionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithCurrentPosition_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var parent = CreateNode(userId, null, "Parent", 0);
        var first = CreateNode(userId, parent.Id, "First", 0);
        var node = CreateNode(userId, parent.Id, "Node", 1);
        var repository = new FakeRepository(parent, first, node);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new ChangeKnowledgeNodePositionCommand(node.Id, parent.Id, 1),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(0);
        node.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithGappedOrders_ShouldNormalizeSequence()
    {
        var userId = Guid.NewGuid();
        var parent = CreateNode(userId, null, "Parent", 0);
        var first = CreateNode(userId, parent.Id, "A1", 0);
        var second = CreateNode(userId, parent.Id, "A2", 5);
        var third = CreateNode(userId, parent.Id, "A3", 12);
        var repository = new FakeRepository(parent, first, second, third);
        var handler = CreateHandler(repository, userId);

        await handler.HandleAsync(
            new ChangeKnowledgeNodePositionCommand(third.Id, parent.Id, 0),
            CancellationToken.None);

        repository.Nodes.Where(node => node.ParentId == parent.Id)
            .OrderBy(node => node.SortOrder)
            .Select(node => node.Title)
            .Should().Equal("A3", "A1", "A2");
        repository.Nodes.Where(node => node.ParentId == parent.Id)
            .Select(node => node.SortOrder)
            .Order()
            .Should().Equal(0, 1, 2);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    private static ChangeKnowledgeNodePositionHandler CreateHandler(
        FakeRepository repository,
        Guid userId) =>
        new(repository, new FakeCurrentUser(userId));

    private static KnowledgeNode CreateNode(
        Guid userId,
        Guid? parentId,
        string title,
        int sortOrder) =>
        KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            parentId,
            title,
            sortOrder,
            DateTimeOffset.UtcNow);

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(params KnowledgeNode[] nodes)
        : IKnowledgeNodeRepository
    {
        public IReadOnlyList<KnowledgeNode> Nodes => nodes;
        public int SaveChangesCallCount { get; private set; }

        public Task<KnowledgeNode?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(nodes.SingleOrDefault(node => node.Id == id));

        public Task<KnowledgeNode?> GetByIdAndUserIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(nodes.SingleOrDefault(node =>
                node.Id == id && node.UserId == userId));

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNode>>(
                nodes.Where(node => node.UserId == userId
                    && node.Status == KnowledgeNodeStatus.Active)
                    .ToList());

        public Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNodeHierarchyItem>>(
                nodes.Where(node => node.UserId == userId)
                    .Select(node => new KnowledgeNodeHierarchyItem(
                        node.Id,
                        node.ParentId))
                    .ToList());

        public Task<int> GetNextSortOrderAsync(
            Guid userId,
            Guid? parentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<KnowledgeNode>> GetActiveSiblingsAsync(
            Guid userId,
            Guid? parentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNode>>(
                nodes.Where(node => node.UserId == userId
                    && node.ParentId == parentId
                    && node.Status == KnowledgeNodeStatus.Active)
                    .OrderBy(node => node.SortOrder)
                    .ThenBy(node => node.Title)
                    .ThenBy(node => node.Id)
                    .ToList());

        public Task<IReadOnlyList<KnowledgeNodeWithTagsItem>> GetActiveByTagIdsAsync(
            Guid userId,
            IReadOnlyCollection<Guid> tagIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<KnowledgeNodeWithTagsItem>>([]);

        public void Add(KnowledgeNode node) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
