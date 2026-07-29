using DevRecall.Application.Analytics;
using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Tests.Common.Time;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class AnalyticsDateRangeResolverTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_ShouldUsePreviousSevenDaysByDefault()
    {
        var result = CreateResolver().Resolve(
            new AnalyticsDateRangeInput(null, null));

        result.FromUtc.Should().Be(Now.AddDays(-7));
        result.ToUtc.Should().Be(Now);
    }

    [Fact]
    public void Resolve_ShouldPreserveExplicitRange()
    {
        var fromUtc = Now.AddDays(-30);
        var toUtc = Now.AddDays(-1);

        var result = CreateResolver().Resolve(
            new AnalyticsDateRangeInput(fromUtc, toUtc));

        result.Should().Be(new AnalyticsDateRange(fromUtc, toUtc));
    }

    [Fact]
    public void Resolve_WithOnlyTo_ShouldDeriveFrom()
    {
        var toUtc = Now.AddDays(-1);
        var result = CreateResolver().Resolve(
            new AnalyticsDateRangeInput(null, toUtc));

        result.FromUtc.Should().Be(toUtc.AddDays(-7));
        result.ToUtc.Should().Be(toUtc);
    }

    [Fact]
    public void Resolve_WithOnlyFrom_ShouldUseClockAsTo()
    {
        var fromUtc = Now.AddDays(-10);
        var result = CreateResolver().Resolve(
            new AnalyticsDateRangeInput(fromUtc, null));

        result.FromUtc.Should().Be(fromUtc);
        result.ToUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_ShouldRejectEqualOrReversedBoundaries(int days)
    {
        var action = () => CreateResolver().Resolve(
            new AnalyticsDateRangeInput(Now, Now.AddDays(days)));

        action.Should().Throw<ValidationException>();
    }

    [Theory]
    [InlineData(365, true)]
    [InlineData(366, false)]
    public void Resolve_ShouldEnforceMaximumRange(
        int days, bool valid)
    {
        var action = () => CreateResolver().Resolve(
            new AnalyticsDateRangeInput(Now.AddDays(-days), Now));

        if (valid)
        {
            action.Should().NotThrow();
        }
        else
        {
            action.Should().Throw<ValidationException>();
        }
    }

    [Fact]
    public void Resolve_ShouldRejectNonUtcTimestamp()
    {
        var action = () => CreateResolver().Resolve(
            new AnalyticsDateRangeInput(
                Now.ToOffset(TimeSpan.FromHours(7)), Now));

        action.Should().Throw<ValidationException>();
    }

    private static AnalyticsDateRangeResolver CreateResolver() =>
        new(new FakeUtcClock(Now));
}
