using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.DsaPerformance;
using DevRecall.Application.Identity;
using DevRecall.Application.Tests.Common.Time;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class DsaPerformanceHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset From =
        new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = From.AddDays(7);

    [Theory]
    [InlineData(10, 4, 40)]
    [InlineData(4, 0, 0)]
    [InlineData(6, 6, 100)]
    public async Task Handler_ShouldCalculateSolvedRate(
        int total, int solved, decimal expected)
    {
        var result = await HandleAsync(new DsaPerformanceReadModel(
            total, solved, total - solved, 0, 0, 3, 34.567m));

        result.SolvedRate.Should().Be(expected);
        result.AverageDurationMinutes.Should().Be(34.57m);
    }

    [Fact]
    public async Task Handler_EmptyState_ShouldReturnZeros()
    {
        var result = await HandleAsync(
            new DsaPerformanceReadModel(0, 0, 0, 0, 0, 0, 0m));

        result.Should().Be(new GetDsaPerformanceResult(
            From, To, 0, 0, 0, 0, 0, 0, 0m, 0m));
    }

    private static async Task<GetDsaPerformanceResult> HandleAsync(
        DsaPerformanceReadModel model)
    {
        var handler = new GetDsaPerformanceHandler(
            new AnalyticsDateRangeResolver(new FakeUtcClock(To)),
            new ReaderStub(model), new CurrentUserStub());
        return await handler.HandleAsync(
            new GetDsaPerformanceQuery(From, To),
            CancellationToken.None);
    }

    private sealed class ReaderStub(DsaPerformanceReadModel result)
        : IDsaPerformanceReader
    {
        public Task<DsaPerformanceReadModel> ReadAsync(
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
        public Guid? UserId => DsaPerformanceHandlerTests.UserId;
    }
}
