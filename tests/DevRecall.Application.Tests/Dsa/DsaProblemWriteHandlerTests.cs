using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Dsa;
using DevRecall.Application.Dsa.Archive;
using DevRecall.Application.Dsa.Update;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using FluentAssertions;

namespace DevRecall.Application.Tests.Dsa;

public sealed class DsaProblemWriteHandlerTests
{
    [Fact]
    public async Task Update_WithEquivalentNormalizedData_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var problem = CreateProblem(userId);
        var repository = new FakeRepository(problem);
        var handler = new UpdateDsaProblemHandler(
            repository, new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new UpdateDsaProblemCommand(
                problem.Id, "  Two   Sum ", " Find two numbers. ", "easy",
                " LeetCode ", null, ["hash table", " ARRAY "]),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(0);
        result.UpdatedAtUtc.Should().Be(problem.CreatedAtUtc);
    }

    [Fact]
    public async Task Archive_WhenAlreadyArchived_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var problem = CreateProblem(userId);
        problem.Archive(DateTimeOffset.UtcNow.AddMinutes(1));
        var archivedAtUtc = problem.UpdatedAtUtc;
        var repository = new FakeRepository(problem);
        var handler = new ArchiveDsaProblemHandler(
            repository, new FakeCurrentUser(userId));

        await handler.HandleAsync(
            new ArchiveDsaProblemCommand(problem.Id),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(0);
        problem.UpdatedAtUtc.Should().Be(archivedAtUtc);
    }

    [Fact]
    public async Task Update_WithChangedTopics_ShouldSaveReplacement()
    {
        var userId = Guid.NewGuid();
        var problem = CreateProblem(userId);
        var repository = new FakeRepository(problem);
        var handler = new UpdateDsaProblemHandler(
            repository, new FakeCurrentUser(userId));

        await handler.HandleAsync(
            new UpdateDsaProblemCommand(
                problem.Id, problem.Title, problem.Description, "Medium",
                null, null, ["Array", "Two Pointers"]),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(1);
        problem.Topics.Select(topic => topic.Name)
            .Should().Equal("Array", "Two Pointers");
    }

    private static DsaProblem CreateProblem(Guid userId) =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, "Two Sum", "Find two numbers.",
            DsaProblemDifficulty.Easy, "LeetCode", null,
            ["Array", "Hash Table"], DateTimeOffset.UtcNow);

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(DsaProblem problem)
        : IDsaProblemRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<DsaProblem?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == problem.Id ? problem : null);

        public Task<DsaProblem?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(id == problem.Id && userId == problem.UserId
                ? problem
                : null);

        public Task<PagedReadResult<DsaProblemListReadItem>>
            GetActiveListAsync(
                Guid userId, DsaProblemDifficulty? difficulty,
                string? normalizedTopic, string? source, int skip, int take,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                new PagedReadResult<DsaProblemListReadItem>([], 0));

        public Task<IReadOnlyList<DsaProblemTopicReadItem>>
            GetTopicsByProblemIdsAsync(
                IReadOnlyCollection<Guid> problemIds,
                CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DsaProblemTopicReadItem>>([]);

        public void Add(DsaProblem item) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
