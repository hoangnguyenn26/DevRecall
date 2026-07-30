using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Domain.Tests.WeakTopics;

public sealed class WeakTopicProfileTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldCaptureCurrentScore()
    {
        var score = Calculate(
            WeaknessSignalType.ReviewAgain,
            WeaknessSignalType.ReviewAgain);

        var profile = CreateProfile(score);

        profile.Score.Should().Be(8m);
        profile.Level.Should().Be(WeaknessLevel.High);
        profile.SignalCount.Should().Be(2);
        profile.CalculatedAtUtc.Should().Be(Now);
        profile.Version.Should().Be(1);
    }

    [Fact]
    public void Recalculate_ShouldUpdateWithNewerScore()
    {
        var profile = CreateProfile(Calculate(
            WeaknessSignalType.ReviewAgain,
            WeaknessSignalType.ReviewAgain));
        var calculatedAt = Now.AddDays(1);
        var score = WeakTopicScoringPolicy.Calculate(
        [
            new WeaknessSignal(
                WeaknessSignalType.ReviewHard, calculatedAt)
        ], calculatedAt);

        profile.Recalculate(score, calculatedAt).Should().BeTrue();
        profile.Score.Should().Be(2m);
        profile.Level.Should().Be(WeaknessLevel.Low);
        profile.UpdatedAtUtc.Should().Be(calculatedAt);
        profile.Version.Should().Be(2);
    }

    [Fact]
    public void Recalculate_SameScore_ShouldBeNoOp()
    {
        var score = Calculate(WeaknessSignalType.ReviewAgain);
        var profile = CreateProfile(score);

        profile.Recalculate(score, Now.AddHours(1)).Should().BeFalse();
        profile.UpdatedAtUtc.Should().Be(Now);
        profile.Version.Should().Be(1);
    }

    [Fact]
    public void Recalculate_ShouldRejectOlderCalculation()
    {
        var profile = CreateProfile(Calculate(
            WeaknessSignalType.ReviewAgain));
        var older = WeakTopicScoringPolicy.Calculate(
        [
            new WeaknessSignal(
                WeaknessSignalType.ReviewAgain, Now.AddDays(-1))
        ], Now.AddDays(-1));

        var action = () => profile.Recalculate(older, Now);

        action.Should().Throw<ArgumentException>();
    }

    private static WeakTopicProfile CreateProfile(
        WeaknessScoreBreakdown score) =>
        WeakTopicProfile.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            WeakTopicResourceType.KnowledgeNode, Guid.NewGuid(), score, Now);

    private static WeaknessScoreBreakdown Calculate(
        params WeaknessSignalType[] types) =>
        WeakTopicScoringPolicy.Calculate(
            types.Select(type => new WeaknessSignal(type, Now)).ToArray(),
            Now);
}
