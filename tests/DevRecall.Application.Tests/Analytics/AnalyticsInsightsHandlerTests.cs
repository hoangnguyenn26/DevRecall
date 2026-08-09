using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.Insights;
using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class AnalyticsInsightsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("7d", 7)]
    [InlineData("30d", 30)]
    [InlineData("90d", 90)]
    public async Task Overview_UsesEqualNonOverlappingPeriods(string value, int days)
    {
        var reader = new ReaderStub(); var handler = Create(reader);
        await handler.GetOverviewAsync(value, CancellationToken.None);
        reader.OverviewRanges.Should().HaveCount(2);
        var periodEnd = new DateTimeOffset(
            Now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        reader.OverviewRanges[0].ToUtc.Should().Be(periodEnd);
        reader.OverviewRanges[0].FromUtc.Should().Be(periodEnd.AddDays(-days));
        reader.OverviewRanges[1].ToUtc.Should().Be(reader.OverviewRanges[0].FromUtc);
        (reader.OverviewRanges[1].ToUtc - reader.OverviewRanges[1].FromUtc).TotalDays.Should().Be(days);
    }

    [Fact]
    public async Task Overview_ReturnsReaderFilledActivityAndComparisons()
    {
        var reader = new ReaderStub { Activity = [new(new(2026, 8, 19), 0, 0), new(new(2026, 8, 20), 30, 2)] };
        var result = await Create(reader).GetOverviewAsync("7d", CancellationToken.None);
        result.Activity.Should().HaveCount(2);
        result.Practice.Difference.Should().Be(6);
    }

    [Fact]
    public async Task InvalidRange_IsRejected()
    {
        var action = () => Create(new ReaderStub()).GetOverviewAsync("365d", CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    private static AnalyticsInsightsHandler Create(ReaderStub reader) => new(reader, new UserStub(), new ClockStub());
    private sealed class ClockStub : IUtcClock { public DateTimeOffset UtcNow => Now; }
    private sealed class UserStub : ICurrentUser { public bool IsAuthenticated => true; public Guid? UserId { get; } = Guid.NewGuid(); }
    private sealed class ReaderStub : IAnalyticsInsightsReader
    {
        public List<AnalyticsDateRange> OverviewRanges { get; } = [];
        public IReadOnlyList<AnalyticsActivityPoint> Activity { get; init; } = [];
        public Task<AnalyticsOverviewAggregate> ReadOverviewAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken)
        {
            OverviewRanges.Add(range); var current = OverviewRanges.Count == 1;
            return Task.FromResult(current ? new AnalyticsOverviewAggregate(30, 1, 1, 2, 2, 2) : new(10, 1, 0, 0, 0, 0));
        }
        public Task<IReadOnlyList<AnalyticsActivityPoint>> ReadActivityAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken) => Task.FromResult(Activity);
        public Task<(RatingDistribution Review, RatingDistribution Interview, RatingDistribution Dsa)> ReadPerformanceAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken) =>
            Task.FromResult<(RatingDistribution, RatingDistribution, RatingDistribution)>(
                (new(0, 0, 0, 0), new(0, 0, 0, 0), new(0, 0, 0, 0)));
    }
}
