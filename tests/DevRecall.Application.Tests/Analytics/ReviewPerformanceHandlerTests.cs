using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.ReviewPerformance;
using DevRecall.Application.Identity;
using DevRecall.Application.Tests.Common.Time;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class ReviewPerformanceHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset From =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Theory]
    [InlineData(20, 10, 3, 65)]
    [InlineData(5, 0, 0, 0)]
    [InlineData(10, 4, 6, 100)]
    public async Task Handler_ShouldCalculateSuccessFromGoodAndEasyOnly(
        int total, int good, int easy, decimal expectedRate)
    {
        var result = await HandleAsync(new ReviewPerformanceReadModel(
            total, total - good - easy, 0, good, easy, 2.345m, 5.678m));

        result.SuccessRate.Should().Be(expectedRate);
        result.AveragePreviousIntervalDays.Should().Be(2.35m);
        result.AverageNextIntervalDays.Should().Be(5.68m);
    }

    [Fact]
    public async Task Handler_EmptyState_ShouldReturnZeros()
    {
        var result = await HandleAsync(
            new ReviewPerformanceReadModel(0, 0, 0, 0, 0, 0m, 0m));

        result.Should().Be(new GetReviewPerformanceResult(
            From, To, 0, 0, 0, 0, 0, 0m, 0m, 0m));
    }

    private static async Task<GetReviewPerformanceResult> HandleAsync(
        ReviewPerformanceReadModel model)
    {
        var handler = new GetReviewPerformanceHandler(
            new AnalyticsDateRangeResolver(new FakeUtcClock(To)),
            new ReaderStub(model), new CurrentUserStub());
        return await handler.HandleAsync(
            new GetReviewPerformanceQuery(From, To),
            CancellationToken.None);
    }

    private sealed class ReaderStub(ReviewPerformanceReadModel result)
        : IReviewPerformanceReader
    {
        public Task<ReviewPerformanceReadModel> ReadAsync(
            Guid userId, AnalyticsDateRange range,
            CancellationToken cancellationToken)
        {
            userId.Should().Be(UserId);
            return Task.FromResult(result);
        }
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => ReviewPerformanceHandlerTests.UserId;
    }
}
