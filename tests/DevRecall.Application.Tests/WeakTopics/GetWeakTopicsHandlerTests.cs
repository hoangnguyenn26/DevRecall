using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics;
using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.WeakTopics;

public sealed class GetWeakTopicsHandlerTests
{
    [Theory]
    [InlineData("high", WeaknessLevel.High)]
    [InlineData("CRIT-ICAL", WeaknessLevel.Critical)]
    [InlineData("me_dium", WeaknessLevel.Medium)]
    public void LevelParser_AcceptsNormalizedNames(
        string value, WeaknessLevel expected) =>
        WeaknessLevelParser.Parse(value).Should().Be(expected);

    [Theory]
    [InlineData("4")]
    [InlineData("Severe")]
    public void LevelParser_RejectsInvalidValues(string value) =>
        FluentActions.Invoking(() => WeaknessLevelParser.Parse(value))
            .Should().Throw<ValidationException>();

    [Fact]
    public async Task Handle_PassesExplicitNoneAndFiltersToReader()
    {
        var context = new Context();

        await context.Handler.HandleAsync(
            new("None", "dsa-problem", 8m, false, 2, 10),
            CancellationToken.None);

        context.ListReader.Level.Should().Be(WeaknessLevel.None);
        context.ListReader.ResourceType.Should().Be(WeakTopicResourceType.DsaProblem);
        context.ListReader.MinimumScore.Should().Be(8m);
        context.ListReader.IncludeNone.Should().BeFalse();
        context.ListReader.Skip.Should().Be(10);
    }

    [Fact]
    public async Task Handle_MapsSummaryAndMissingResourceFallback()
    {
        var context = new Context();
        var first = Profile(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var second = Profile(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        context.ListReader.Result = new([first, second], 2);
        context.SummaryReader.Items =
        [
            new(first.ResourceType, first.ResourceId, "Available", "Preview", true)
        ];

        var result = await context.Handler.HandleAsync(
            new(null, null, null, false, 1, 20), CancellationToken.None);

        result.Items[0].ResourceTitle.Should().Be("Available");
        result.Items[1].ResourceTitle.Should().Be("Unavailable resource");
        result.Items[1].IsResourceAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ComputesPaginationMetadata()
    {
        var context = new Context();
        context.ListReader.Result = new([], 25);

        var result = await context.Handler.HandleAsync(
            new(null, null, null, false, 2, 10), CancellationToken.None);

        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 101, 0)]
    [InlineData(1, 20, -0.01)]
    public async Task Handle_RejectsInvalidQuery(
        int page, int pageSize, decimal minimumScore)
    {
        var context = new Context();

        var action = () => context.Handler.HandleAsync(
            new(null, null, minimumScore, false, page, pageSize),
            CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    private static WeakTopicListReadModel Profile(Guid id) => new(
        id, WeakTopicResourceType.KnowledgeNode, Guid.NewGuid(), 4m,
        WeaknessLevel.Medium, 1, 1,
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));

    private sealed class Context
    {
        public ListReaderStub ListReader { get; } = new();
        public SummaryReaderStub SummaryReader { get; } = new();
        public GetWeakTopicsHandler Handler => new(
            ListReader, SummaryReader, new CurrentUserStub());
    }

    private sealed class ListReaderStub : IWeakTopicListReader
    {
        public PagedReadResult<WeakTopicListReadModel> Result { get; set; } = new([], 0);
        public WeaknessLevel? Level { get; private set; }
        public WeakTopicResourceType? ResourceType { get; private set; }
        public decimal? MinimumScore { get; private set; }
        public bool IncludeNone { get; private set; }
        public int Skip { get; private set; }
        public Task<PagedReadResult<WeakTopicListReadModel>> ReadAsync(
            Guid userId, WeaknessLevel? level, WeakTopicResourceType? resourceType,
            decimal? minimumScore, bool includeNone, int skip, int take,
            CancellationToken cancellationToken)
        {
            Level = level;
            ResourceType = resourceType;
            MinimumScore = minimumScore;
            IncludeNone = includeNone;
            Skip = skip;
            return Task.FromResult(Result);
        }
    }

    private sealed class SummaryReaderStub : IWeakTopicResourceSummaryReader
    {
        public IReadOnlyList<WeakTopicResourceSummary> Items { get; set; } = [];
        public Task<IReadOnlyList<WeakTopicResourceSummary>> ReadManyAsync(
            Guid userId, IReadOnlyCollection<WeakTopicResourceReference> resources,
            CancellationToken cancellationToken) => Task.FromResult(Items);
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
    }
}
