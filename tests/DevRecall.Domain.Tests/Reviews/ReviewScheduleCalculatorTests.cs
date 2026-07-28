using DevRecall.Domain.Reviews;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Reviews;

public sealed class ReviewScheduleCalculatorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ReviewEvaluation.Again, 1)]
    [InlineData(ReviewEvaluation.Hard, 1)]
    [InlineData(ReviewEvaluation.Good, 2)]
    [InlineData(ReviewEvaluation.Easy, 4)]
    public void Calculate_ShouldReturnExpectedFirstInterval(
        ReviewEvaluation evaluation, int expectedInterval)
    {
        var result = ReviewScheduleCalculator.Calculate(
            0, Now, evaluation, Now);

        result.NextIntervalDays.Should().Be(expectedInterval);
        result.NextDueAtUtc.Should().Be(Now.AddDays(expectedInterval));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 5)]
    [InlineData(4, 6)]
    public void Hard_ShouldRoundAwayFromZero(
        int currentInterval, int expectedInterval)
    {
        var result = ReviewScheduleCalculator.Calculate(
            currentInterval, Now, ReviewEvaluation.Hard, Now);

        result.NextIntervalDays.Should().Be(expectedInterval);
    }

    [Theory]
    [InlineData(ReviewEvaluation.Again, 0, 1)]
    [InlineData(ReviewEvaluation.Again, 4, 1)]
    [InlineData(ReviewEvaluation.Hard, 0, 1)]
    [InlineData(ReviewEvaluation.Hard, 1, 2)]
    [InlineData(ReviewEvaluation.Hard, 2, 3)]
    [InlineData(ReviewEvaluation.Hard, 3, 5)]
    [InlineData(ReviewEvaluation.Hard, 4, 6)]
    [InlineData(ReviewEvaluation.Good, 0, 2)]
    [InlineData(ReviewEvaluation.Good, 1, 2)]
    [InlineData(ReviewEvaluation.Good, 2, 4)]
    [InlineData(ReviewEvaluation.Good, 4, 8)]
    [InlineData(ReviewEvaluation.Easy, 0, 4)]
    [InlineData(ReviewEvaluation.Easy, 1, 4)]
    [InlineData(ReviewEvaluation.Easy, 2, 6)]
    [InlineData(ReviewEvaluation.Easy, 4, 12)]
    public void Calculate_ShouldProtectSchedulerMatrix(
        ReviewEvaluation evaluation,
        int currentInterval,
        int expectedInterval)
    {
        var result = ReviewScheduleCalculator.Calculate(
            currentInterval, Now, evaluation, Now);

        result.NextIntervalDays.Should().Be(expectedInterval);
    }

    [Fact]
    public void Calculate_ShouldUseActualReviewTimeForNextDueDate()
    {
        var reviewedEarly = Now.AddDays(-1);

        var result = ReviewScheduleCalculator.Calculate(
            2, Now, ReviewEvaluation.Good, reviewedEarly);

        result.PreviousDueAtUtc.Should().Be(Now);
        result.NextDueAtUtc.Should().Be(reviewedEarly.AddDays(4));
    }

    [Fact]
    public void Calculate_WithInvalidInputs_ShouldThrow()
    {
        var invalidEvaluation = () => ReviewScheduleCalculator.Calculate(
            0, Now, (ReviewEvaluation)0, Now);
        var negativeInterval = () => ReviewScheduleCalculator.Calculate(
            -1, Now, ReviewEvaluation.Good, Now);
        var nonUtc = () => ReviewScheduleCalculator.Calculate(
            0, Now.ToOffset(TimeSpan.FromHours(7)),
            ReviewEvaluation.Good, Now);

        invalidEvaluation.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{ReviewErrors.InvalidEvaluation.Message}*");
        negativeInterval.Should().Throw<ArgumentOutOfRangeException>();
        nonUtc.Should().Throw<ArgumentException>()
            .WithMessage("*currentDueAtUtc must be in UTC.*");
    }
}
