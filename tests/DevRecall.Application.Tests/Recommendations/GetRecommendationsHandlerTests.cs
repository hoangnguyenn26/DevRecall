using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.Recommendations;

public sealed class GetRecommendationsHandlerTests
{
    [Fact]
    public async Task Handle_DefaultsToActiveAndPassesCombinedFilters()
    {
        var context = new Context();

        await context.Handler.HandleAsync(
            new(null, "High", "dsa-problem", "retry_dsa_problem", 9m, 2, 10),
            CancellationToken.None);

        context.Reader.Status.Should().Be(RecommendationStatus.Active);
        context.Reader.Priority.Should().Be(RecommendationPriority.High);
        context.Reader.ResourceType.Should().Be(
            RecommendationResourceType.DsaProblem);
        context.Reader.Type.Should().Be(RecommendationType.RetryDsaProblem);
        context.Reader.MinimumScore.Should().Be(9m);
        context.Reader.Skip.Should().Be(10);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("+1")]
    [InlineData("01")]
    public void Parsers_RejectNumericRepresentations(string value)
    {
        FluentActions.Invoking(() =>
            RecommendationParsers.ParseStatus(value))
            .Should().Throw<ValidationException>();
        FluentActions.Invoking(() =>
            RecommendationParsers.ParsePriority(value))
            .Should().Throw<ValidationException>();
        FluentActions.Invoking(() =>
            RecommendationParsers.ParseResourceType(value))
            .Should().Throw<ValidationException>();
        FluentActions.Invoking(() =>
            RecommendationParsers.ParseType(value))
            .Should().Throw<ValidationException>();
    }

    [Fact]
    public async Task Handle_MapsSummaryAndMissingFallback()
    {
        var context = new Context();
        var first = Item(Guid.NewGuid(), Guid.NewGuid());
        var second = Item(Guid.NewGuid(), Guid.NewGuid());
        context.Reader.Result = new([first, second], 2);
        context.Summaries.Items =
        [
            new(first.ResourceType, first.ResourceId, "Problem", "Preview", true)
        ];

        var result = await context.Handler.HandleAsync(
            new(null, null, null, null, null, 1, 20), CancellationToken.None);

        result.Items[0].ResourceTitle.Should().Be("Problem");
        result.Items[1].ResourceTitle.Should().Be("Unavailable resource");
        result.Items[1].IsResourceAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ComputesPagination()
    {
        var context = new Context();
        context.Reader.Result = new([], 25);

        var result = await context.Handler.HandleAsync(
            new(null, null, null, null, null, 2, 10), CancellationToken.None);

        result.TotalPages.Should().Be(3);
        result.TotalCount.Should().Be(25);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 101, 0)]
    [InlineData(1, 20, -0.01)]
    public async Task Handle_RejectsInvalidQuery(
        int page, int pageSize, decimal score)
    {
        var context = new Context();
        var action = () => context.Handler.HandleAsync(
            new(null, null, null, null, score, page, pageSize),
            CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    private static RecommendationListReadModel Item(Guid id, Guid resourceId) =>
        new(
            id, RecommendationResourceType.DsaProblem, resourceId,
            RecommendationType.RetryDsaProblem, RecommendationPriority.High,
            9m, RecommendationStatus.Active, 9m, WeaknessLevel.High, 2,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero),
            null, null, null, null, null, 1);

    private sealed class Context
    {
        public ReaderStub Reader { get; } = new();
        public SummaryStub Summaries { get; } = new();
        public GetRecommendationsHandler Handler => new(
            Reader, Summaries, new CurrentUserStub());
    }

    private sealed class ReaderStub : IRecommendationListReader
    {
        public PagedReadResult<RecommendationListReadModel> Result { get; set; } =
            new([], 0);
        public RecommendationStatus Status { get; private set; }
        public RecommendationPriority? Priority { get; private set; }
        public RecommendationResourceType? ResourceType { get; private set; }
        public RecommendationType? Type { get; private set; }
        public decimal? MinimumScore { get; private set; }
        public int Skip { get; private set; }
        public Task<PagedReadResult<RecommendationListReadModel>> ReadAsync(
            Guid userId, RecommendationStatus status,
            RecommendationPriority? priority,
            RecommendationResourceType? resourceType, RecommendationType? type,
            decimal? minimumPriorityScore, int skip, int take,
            CancellationToken cancellationToken)
        {
            Status = status;
            Priority = priority;
            ResourceType = resourceType;
            Type = type;
            MinimumScore = minimumPriorityScore;
            Skip = skip;
            return Task.FromResult(Result);
        }
    }

    private sealed class SummaryStub : IRecommendationResourceSummaryReader
    {
        public IReadOnlyList<RecommendationResourceSummary> Items { get; set; } = [];
        public Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<RecommendationResourceReference> resources,
            CancellationToken cancellationToken) => Task.FromResult(Items);
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
    }
}
