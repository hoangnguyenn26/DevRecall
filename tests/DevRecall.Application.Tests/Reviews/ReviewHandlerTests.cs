using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Reviews;
using DevRecall.Application.Reviews.Create;
using DevRecall.Application.Reviews.GetDue;
using DevRecall.Application.Reviews.Resources;
using DevRecall.Application.Tests.Common.Time;
using DevRecall.Domain.Reviews;
using FluentAssertions;

namespace DevRecall.Application.Tests.Reviews;

public sealed class ReviewHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("knowledge node", ReviewResourceType.KnowledgeNode)]
    [InlineData("interview_question", ReviewResourceType.InterviewQuestion)]
    [InlineData("DSA-PROBLEM", ReviewResourceType.DsaProblem)]
    public async Task Create_ShouldParseTypeAndCreateItemDueNow(
        string value, ReviewResourceType expectedType)
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var repository = new ReviewItemRepositoryStub();
        var resolver = new ResolverStub(new ReviewResourceResolution(
            expectedType, resourceId, "Title", null,
            ReviewResourceAvailability.Available));
        var handler = new CreateReviewItemHandler(
            repository, resolver, new CurrentUserStub(userId),
            new FakeUtcClock(Now));

        var result = await handler.HandleAsync(
            new CreateReviewItemCommand(value, resourceId),
            CancellationToken.None);

        result.ResourceType.Should().Be(expectedType.ToString());
        result.Status.Should().Be("Active");
        result.DueAtUtc.Should().Be(Now);
        result.IntervalDays.Should().Be(0);
        result.ReviewCount.Should().Be(0);
        repository.Added.Should().NotBeNull();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Due_ShouldIncludeBoundaryOrderItemsAndUseFallback()
    {
        var userId = Guid.NewGuid();
        var oldestId = Guid.NewGuid();
        var boundaryId = Guid.NewGuid();
        var repository = new ReviewItemRepositoryStub
        {
            DueResult = new PagedReadResult<DueReviewItemReadModel>(
                [
                    Due(oldestId, Now.AddMinutes(-119.9)),
                    Due(boundaryId, Now)
                ],
                2)
        };
        var summaryReader = new SummaryReaderStub(
            [
                new ReviewResourceSummary(
                    ReviewResourceType.DsaProblem,
                    oldestId,
                    "Oldest",
                    "Preview")
            ]);
        var handler = new GetDueReviewItemsHandler(
            repository, summaryReader, new CurrentUserStub(userId),
            new FakeUtcClock(Now));

        var result = await handler.HandleAsync(
            new GetDueReviewItemsQuery(null, 1, 20),
            CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
        result.Items.Select(item => item.ResourceId)
            .Should().Equal(oldestId, boundaryId);
        result.Items[0].OverdueMinutes.Should().Be(119);
        result.Items[1].OverdueMinutes.Should().Be(0);
        result.Items[1].ResourceTitle.Should().Be("Unavailable resource");
        repository.DueAtOrBeforeUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Due_WithInvalidPagination_ShouldThrow(
        int page, int pageSize)
    {
        var handler = new GetDueReviewItemsHandler(
            new ReviewItemRepositoryStub(), new SummaryReaderStub([]),
            new CurrentUserStub(Guid.NewGuid()), new FakeUtcClock(Now));

        var action = () => handler.HandleAsync(
            new GetDueReviewItemsQuery(null, page, pageSize),
            CancellationToken.None);

        await action.Should().ThrowAsync<
            Application.Common.Exceptions.ValidationException>();
    }

    private static DueReviewItemReadModel Due(
        Guid resourceId, DateTimeOffset dueAt) =>
        new(
            Guid.NewGuid(), ReviewResourceType.DsaProblem, resourceId,
            dueAt, null, 0, 0);

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class ResolverStub(ReviewResourceResolution? result)
        : IReviewResourceResolver
    {
        public Task<ReviewResourceResolution?> ResolveAsync(
            Guid userId, ReviewResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class SummaryReaderStub(
        IReadOnlyList<ReviewResourceSummary> results)
        : IReviewResourceSummaryReader
    {
        public Task<IReadOnlyList<ReviewResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<ReviewResourceReference> resources,
            CancellationToken cancellationToken) =>
            Task.FromResult(results);
    }

    private sealed class ReviewItemRepositoryStub : IReviewItemRepository
    {
        public ReviewItem? Added { get; private set; }
        public int SaveCount { get; private set; }
        public DateTimeOffset? DueAtOrBeforeUtc { get; private set; }
        public PagedReadResult<DueReviewItemReadModel> DueResult { get; init; } =
            new([], 0);

        public Task<ReviewItem?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReviewItem?>(null);

        public Task<ReviewItem?> GetByIdAndUserIdAsync(
            Guid id, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<ReviewItem?>(null);

        public Task<ReviewItem?> GetByIdAndUserIdForUpdateAsync(
            Guid id, Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<ReviewItem?>(null);

        public Task<bool> ActiveExistsAsync(
            Guid userId, ReviewResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(ReviewItem reviewItem) => Added = reviewItem;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<PagedReadResult<DueReviewItemReadModel>> GetDueAsync(
            Guid userId, DateTimeOffset dueAtOrBeforeUtc,
            ReviewResourceType? resourceType, int skip, int take,
            CancellationToken cancellationToken)
        {
            DueAtOrBeforeUtc = dueAtOrBeforeUtc;
            return Task.FromResult(DueResult);
        }
    }
}
