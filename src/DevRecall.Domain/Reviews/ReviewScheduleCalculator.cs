namespace DevRecall.Domain.Reviews;

public static class ReviewScheduleCalculator
{
    public static ReviewSchedule Calculate(
        int currentIntervalDays,
        DateTimeOffset currentDueAtUtc,
        ReviewEvaluation evaluation,
        DateTimeOffset reviewedAtUtc)
    {
        if (currentIntervalDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentIntervalDays), "Current interval cannot be negative.");
        }

        EnsureValidEvaluation(evaluation);
        EnsureUtc(currentDueAtUtc, nameof(currentDueAtUtc));
        EnsureUtc(reviewedAtUtc, nameof(reviewedAtUtc));

        var nextIntervalDays = CalculateNextInterval(
            currentIntervalDays, evaluation);

        return new ReviewSchedule(
            currentIntervalDays, nextIntervalDays, currentDueAtUtc,
            reviewedAtUtc, reviewedAtUtc.AddDays(nextIntervalDays));
    }

    private static int CalculateNextInterval(
        int currentIntervalDays, ReviewEvaluation evaluation) =>
        evaluation switch
        {
            ReviewEvaluation.Again => 1,
            ReviewEvaluation.Hard => Math.Max(
                1, (int)Math.Round(
                    currentIntervalDays * 1.5,
                    MidpointRounding.AwayFromZero)),
            ReviewEvaluation.Good => Math.Max(
                2, checked(currentIntervalDays * 2)),
            ReviewEvaluation.Easy => Math.Max(
                4, checked(currentIntervalDays * 3)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(evaluation), ReviewErrors.InvalidEvaluation.Message)
        };

    private static void EnsureValidEvaluation(ReviewEvaluation evaluation)
    {
        if (!Enum.IsDefined(evaluation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(evaluation), ReviewErrors.InvalidEvaluation.Message);
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"{parameterName} must be in UTC.", parameterName);
        }
    }
}
