using DevRecall.Domain.Reviews;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Reviews;

public sealed class ReviewHistoryTests
{
    [Fact]
    public void Create_ShouldCopyImmutableScheduleSnapshot()
    {
        var reviewedAt = new DateTimeOffset(
            2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var previousDue = reviewedAt.AddHours(-1);
        var schedule = new ReviewSchedule(
            0, 2, previousDue, reviewedAt, reviewedAt.AddDays(2));

        var history = ReviewHistory.Create(
            Guid.NewGuid(), Guid.NewGuid(), ReviewEvaluation.Good,
            schedule, reviewedAt);

        history.Evaluation.Should().Be(ReviewEvaluation.Good);
        history.PreviousIntervalDays.Should().Be(0);
        history.NextIntervalDays.Should().Be(2);
        history.PreviousDueAtUtc.Should().Be(previousDue);
        history.NextDueAtUtc.Should().Be(reviewedAt.AddDays(2));
        history.ReviewedAtUtc.Should().Be(reviewedAt);
        history.CreatedAtUtc.Should().Be(reviewedAt);
    }

    [Fact]
    public void Create_WithInvalidSchedule_ShouldThrow()
    {
        var now = new DateTimeOffset(
            2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        var invalidSchedule = new ReviewSchedule(
            0, 0, now, now, now);

        var action = () => ReviewHistory.Create(
            Guid.NewGuid(), Guid.NewGuid(), ReviewEvaluation.Good,
            invalidSchedule, now);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Next interval must be greater than zero.*");
    }
}
