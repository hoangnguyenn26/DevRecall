using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Domain.Tests.WeakTopics;

public sealed class WeakTopicScoringPolicyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(WeaknessSignalType.ReviewAgain, 4)]
    [InlineData(WeaknessSignalType.ReviewHard, 2)]
    [InlineData(WeaknessSignalType.ReviewGood, -2)]
    [InlineData(WeaknessSignalType.ReviewEasy, -3)]
    [InlineData(WeaknessSignalType.DsaFailed, 4)]
    [InlineData(WeaknessSignalType.DsaPartiallySolved, 2)]
    [InlineData(WeaknessSignalType.DsaSolved, -4)]
    [InlineData(WeaknessSignalType.DsaSkipped, 3)]
    [InlineData(WeaknessSignalType.StudyItemCompleted, -1)]
    [InlineData(WeaknessSignalType.StudyItemSkipped, 2)]
    public void GetWeight_ShouldReturnExpectedValue(
        WeaknessSignalType signal, int expected)
    {
        WeakTopicScoringPolicy.GetWeight(signal).Should().Be(expected);
    }

    [Theory]
    [InlineData(7, 1.00)]
    [InlineData(8, 0.60)]
    [InlineData(30, 0.60)]
    [InlineData(31, 0.25)]
    [InlineData(90, 0.25)]
    [InlineData(91, 0.00)]
    public void RecencyMultiplier_ShouldFollowBucketRules(
        int ageDays, decimal expected)
    {
        WeakTopicScoringPolicy.GetRecencyMultiplier(
            Now.AddDays(-ageDays), Now).Should().Be(expected);
    }

    [Fact]
    public void RecencyMultiplier_ShouldRejectFutureAndNonUtcSignals()
    {
        var future = () => WeakTopicScoringPolicy.GetRecencyMultiplier(
            Now.AddSeconds(1), Now);
        var nonUtc = () => WeakTopicScoringPolicy.GetRecencyMultiplier(
            Now.ToOffset(TimeSpan.FromHours(7)), Now);

        future.Should().Throw<ArgumentException>();
        nonUtc.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calculate_ShouldCombinePositiveAndNegativeSignals()
    {
        var result = WeakTopicScoringPolicy.Calculate(
        [
            new WeaknessSignal(WeaknessSignalType.ReviewAgain, Now),
            new WeaknessSignal(WeaknessSignalType.ReviewHard, Now),
            new WeaknessSignal(WeaknessSignalType.ReviewGood, Now)
        ], Now);

        result.RawScore.Should().Be(4m);
        result.FinalScore.Should().Be(4m);
        result.Level.Should().Be(WeaknessLevel.Medium);
        result.SignalCount.Should().Be(3);
    }

    [Fact]
    public void Calculate_ShouldClampNegativeScoreToZero()
    {
        var result = WeakTopicScoringPolicy.Calculate(
        [
            new WeaknessSignal(WeaknessSignalType.ReviewEasy, Now),
            new WeaknessSignal(WeaknessSignalType.DsaSolved, Now)
        ], Now);

        result.RawScore.Should().Be(-7m);
        result.FinalScore.Should().Be(0m);
        result.Level.Should().Be(WeaknessLevel.None);
    }

    [Fact]
    public void Calculate_ShouldExcludeSignalsOlderThanNinetyDays()
    {
        var result = WeakTopicScoringPolicy.Calculate(
        [
            new WeaknessSignal(
                WeaknessSignalType.ReviewAgain, Now.AddDays(-91))
        ], Now);

        result.FinalScore.Should().Be(0m);
        result.SignalCount.Should().Be(0);
        result.Contributions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, WeaknessLevel.None)]
    [InlineData(0.01, WeaknessLevel.Low)]
    [InlineData(3.99, WeaknessLevel.Low)]
    [InlineData(4, WeaknessLevel.Medium)]
    [InlineData(7.99, WeaknessLevel.Medium)]
    [InlineData(8, WeaknessLevel.High)]
    [InlineData(11.99, WeaknessLevel.High)]
    [InlineData(12, WeaknessLevel.Critical)]
    public void GetLevel_ShouldApplyThresholds(
        decimal score, WeaknessLevel expected)
    {
        WeakTopicScoringPolicy.GetLevel(score).Should().Be(expected);
    }
}
