using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.DailyActivity;
using DevRecall.Application.Identity;
using DevRecall.Application.Tests.Common.Time;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class DailyActivityHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset From =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To =
        new(2026, 8, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DateRangeExtensions_ShouldIncludePartialFirstAndLastDates()
    {
        var range = new AnalyticsDateRange(From, To);

        range.GetFirstUtcDate().Should().Be(new DateOnly(2026, 8, 1));
        range.GetLastIncludedUtcDate().Should().Be(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public void DateRangeExtensions_ShouldExcludeMidnightUpperBoundaryDate()
    {
        var range = new AnalyticsDateRange(
            From, new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));

        range.GetLastIncludedUtcDate().Should().Be(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public async Task Handler_ShouldSortAndZeroFillMissingDates()
    {
        var reader = new ReaderStub(
        [
            new DailyActivityAggregate(
                new DateOnly(2026, 8, 3), 60, 1, 3, 2, 2),
            new DailyActivityAggregate(
                new DateOnly(2026, 8, 1), 45, 1, 2, 3, 1)
        ]);
        var handler = new GetDailyActivityHandler(
            new AnalyticsDateRangeResolver(new FakeUtcClock(To)),
            reader, new CurrentUserStub());

        var result = await handler.HandleAsync(
            new GetDailyActivityQuery(From, To),
            CancellationToken.None);

        result.Days.Select(day => day.Date).Should().Equal(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 2),
            new DateOnly(2026, 8, 3));
        result.Days[1].Should().Be(
            new DailyActivityDay(
                new DateOnly(2026, 8, 2), 0, 0, 0, 0, 0));
        reader.UserId.Should().Be(UserId);
    }

    private sealed class ReaderStub(
        IReadOnlyList<DailyActivityAggregate> result)
        : IDailyActivityReader
    {
        public Guid UserId { get; private set; }

        public Task<IReadOnlyList<DailyActivityAggregate>> ReadAsync(
            Guid userId, AnalyticsDateRange range,
            CancellationToken cancellationToken)
        {
            UserId = userId;
            return Task.FromResult(result);
        }
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => DailyActivityHandlerTests.UserId;
    }
}
