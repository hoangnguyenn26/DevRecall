using DevRecall.Domain.Reviews;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Reviews;

public sealed class ReviewItemTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldInitializeActiveDueItem()
    {
        var item = CreateItem();

        item.Status.Should().Be(ReviewItemStatus.Active);
        item.DueAtUtc.Should().Be(CreatedAt);
        item.LastReviewedAtUtc.Should().BeNull();
        item.IntervalDays.Should().Be(0);
        item.ReviewCount.Should().Be(0);
        item.CreatedAtUtc.Should().Be(CreatedAt);
        item.UpdatedAtUtc.Should().Be(CreatedAt);
    }

    [Fact]
    public void Evaluate_ShouldUpdateScheduleAndReviewCount()
    {
        var item = CreateItem();
        var reviewedAt = CreatedAt.AddHours(1);

        var schedule = item.Evaluate(ReviewEvaluation.Good, 0, reviewedAt);

        schedule.PreviousIntervalDays.Should().Be(0);
        schedule.PreviousDueAtUtc.Should().Be(CreatedAt);
        item.IntervalDays.Should().Be(2);
        item.DueAtUtc.Should().Be(reviewedAt.AddDays(2));
        item.LastReviewedAtUtc.Should().Be(reviewedAt);
        item.ReviewCount.Should().Be(1);
        item.UpdatedAtUtc.Should().Be(reviewedAt);
    }

    [Fact]
    public void Archive_ShouldBeIdempotentWithoutChangingTimestampOnNoOp()
    {
        var item = CreateItem();
        var archivedAt = CreatedAt.AddHours(1);

        item.Archive(archivedAt).Should().BeTrue();
        item.Archive(archivedAt.AddHours(1)).Should().BeFalse();

        item.Status.Should().Be(ReviewItemStatus.Archived);
        item.UpdatedAtUtc.Should().Be(archivedAt);
    }

    [Fact]
    public void Evaluate_WhenArchived_ShouldThrow()
    {
        var item = CreateItem();
        item.Archive(CreatedAt.AddHours(1));

        var action = () => item.Evaluate(
            ReviewEvaluation.Good, 0, CreatedAt.AddHours(2));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(ReviewErrors.ItemArchived.Message);
    }

    [Fact]
    public void Evaluate_WithStaleReviewCount_ShouldNotChangeState()
    {
        var item = CreateItem();

        var action = () => item.Evaluate(
            ReviewEvaluation.Good, 1, CreatedAt.AddHours(1));

        action.Should().Throw<ReviewScheduleConflictException>()
            .WithMessage(ReviewErrors.ScheduleConflict.Message);
        item.IntervalDays.Should().Be(0);
        item.ReviewCount.Should().Be(0);
        item.DueAtUtc.Should().Be(CreatedAt);
        item.LastReviewedAtUtc.Should().BeNull();
        item.UpdatedAtUtc.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_WithInvalidIdentityResourceTypeOrTime_ShouldThrow()
    {
        var emptyId = () => ReviewItem.Create(
            Guid.Empty, Guid.NewGuid(), ReviewResourceType.DsaProblem,
            Guid.NewGuid(), CreatedAt, CreatedAt);
        var invalidType = () => ReviewItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), (ReviewResourceType)0,
            Guid.NewGuid(), CreatedAt, CreatedAt);
        var nonUtc = () => ReviewItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), ReviewResourceType.DsaProblem,
            Guid.NewGuid(), CreatedAt.ToOffset(TimeSpan.FromHours(7)), CreatedAt);

        emptyId.Should().Throw<ArgumentException>();
        invalidType.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{ReviewErrors.InvalidResourceType.Message}*");
        nonUtc.Should().Throw<ArgumentException>()
            .WithMessage("*dueAtUtc must be in UTC.*");
    }

    private static ReviewItem CreateItem() =>
        ReviewItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), ReviewResourceType.DsaProblem,
            Guid.NewGuid(), CreatedAt, CreatedAt);
}
