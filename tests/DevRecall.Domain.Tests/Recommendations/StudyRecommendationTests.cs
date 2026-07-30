using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Recommendations;

public sealed class StudyRecommendationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(WeaknessLevel.Low, RecommendationPriority.Low)]
    [InlineData(WeaknessLevel.Medium, RecommendationPriority.Medium)]
    [InlineData(WeaknessLevel.High, RecommendationPriority.High)]
    [InlineData(WeaknessLevel.Critical, RecommendationPriority.Critical)]
    public void PriorityPolicy_ShouldMapActiveWeakness(
        WeaknessLevel level, RecommendationPriority priority) =>
        RecommendationPriorityPolicy.FromWeaknessLevel(level).Should().Be(priority);

    [Fact]
    public void PriorityPolicy_ShouldRejectNone() =>
        FluentActions.Invoking(() =>
            RecommendationPriorityPolicy.FromWeaknessLevel(WeaknessLevel.None))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void Create_ShouldSetInitialState()
    {
        var recommendation = Create();

        recommendation.Status.Should().Be(RecommendationStatus.Active);
        recommendation.PriorityScore.Should().Be(8m);
        recommendation.Version.Should().Be(1);
        recommendation.GeneratedAtUtc.Should().Be(Now);
        recommendation.DismissedAtUtc.Should().BeNull();
        recommendation.CompletedAtUtc.Should().BeNull();
        recommendation.ExpiredAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRejectMismatchedResourceAndType()
    {
        var action = () => StudyRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationResourceType.KnowledgeNode,
            Guid.NewGuid(), RecommendationType.RetryDsaProblem,
            RecommendationPriority.High, Reason(), Now, Now.AddDays(14));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Refresh_ShouldUpdateChangedRecommendation()
    {
        var recommendation = Create();
        var reason = RecommendationReason.Create(
            12m, WeaknessLevel.Critical, 4, Now.AddDays(1));

        recommendation.Refresh(
            RecommendationPriority.Critical, reason, Now.AddDays(1),
            Now.AddDays(15)).Should().BeTrue();

        recommendation.Priority.Should().Be(RecommendationPriority.Critical);
        recommendation.PriorityScore.Should().Be(12m);
        recommendation.Version.Should().Be(2);
    }

    [Fact]
    public void Refresh_WithSameState_ShouldBeNoOp()
    {
        var recommendation = Create();

        recommendation.Refresh(
            recommendation.Priority, recommendation.Reason, Now.AddHours(1),
            recommendation.ExpiresAtUtc).Should().BeFalse();

        recommendation.Version.Should().Be(1);
        recommendation.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Dismiss_ShouldMakeRecommendationTerminal()
    {
        var recommendation = Create();

        recommendation.Dismiss(recommendation.Version, Now.AddHours(1));

        recommendation.Status.Should().Be(RecommendationStatus.Dismissed);
        recommendation.DismissedAtUtc.Should().Be(Now.AddHours(1));
        FluentActions.Invoking(() => recommendation.Complete(
            recommendation.Version, Now.AddHours(2)))
            .Should().Throw<RecommendationDomainException>();
    }

    [Fact]
    public void Complete_ShouldMakeRecommendationTerminal()
    {
        var recommendation = Create();

        recommendation.Complete(recommendation.Version, Now.AddHours(1));

        recommendation.Status.Should().Be(RecommendationStatus.Completed);
        recommendation.CompletedAtUtc.Should().Be(Now.AddHours(1));
        FluentActions.Invoking(() => recommendation.Refresh(
            recommendation.Priority, recommendation.Reason, Now.AddHours(2),
            recommendation.ExpiresAtUtc))
            .Should().Throw<RecommendationDomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Expire_ShouldRespectBoundary(int offsetSeconds)
    {
        var recommendation = Create();
        var expiration = recommendation.ExpiresAtUtc!.Value;

        recommendation.Expire(
            recommendation.Version,
            RecommendationExpirationReason.LifetimeElapsed,
            expiration.AddSeconds(offsetSeconds))
            .Should().BeTrue();

        recommendation.Status.Should().Be(RecommendationStatus.Expired);
    }

    [Fact]
    public void Expire_LifetimeBeforeBoundaryShouldReject()
    {
        var recommendation = Create();

        FluentActions.Invoking(() => recommendation.Expire(
            recommendation.Version,
            RecommendationExpirationReason.LifetimeElapsed,
            recommendation.ExpiresAtUtc!.Value.AddSeconds(-1)))
            .Should().Throw<RecommendationDomainException>()
            .Where(x => x.Error == RecommendationErrors.NotExpiredYet);
    }

    [Fact]
    public void Reason_ShouldValidateActiveWeaknessAndUtc()
    {
        FluentActions.Invoking(() => RecommendationReason.Create(
            0, WeaknessLevel.None, -1, Now.ToOffset(TimeSpan.FromHours(7))))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    private static StudyRecommendation Create() =>
        StudyRecommendation.Create(
            Guid.NewGuid(), Guid.NewGuid(), RecommendationResourceType.DsaProblem,
            Guid.NewGuid(), RecommendationType.RetryDsaProblem,
            RecommendationPriority.High, Reason(), Now, Now.AddDays(14));

    private static RecommendationReason Reason() =>
        RecommendationReason.Create(8m, WeaknessLevel.High, 3, Now);
}
