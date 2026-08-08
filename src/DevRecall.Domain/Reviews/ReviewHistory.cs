namespace DevRecall.Domain.Reviews;

public sealed class ReviewHistory
{
    private ReviewHistory()
    {
    }

    private ReviewHistory(
        Guid id, Guid reviewItemId, ReviewEvaluation evaluation,
        int previousIntervalDays, int nextIntervalDays,
        DateTimeOffset previousDueAtUtc, DateTimeOffset nextDueAtUtc,
        DateTimeOffset reviewedAtUtc, DateTimeOffset createdAtUtc,
        Guid? userId = null, Guid? submissionId = null,
        int? resultReviewCount = null)
    {
        Id = id;
        ReviewItemId = reviewItemId;
        Evaluation = evaluation;
        PreviousIntervalDays = previousIntervalDays;
        NextIntervalDays = nextIntervalDays;
        PreviousDueAtUtc = previousDueAtUtc;
        NextDueAtUtc = nextDueAtUtc;
        ReviewedAtUtc = reviewedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UserId = userId;
        SubmissionId = submissionId;
        ResultReviewCount = resultReviewCount;
    }

    public Guid Id { get; private set; }
    public Guid ReviewItemId { get; private set; }
    public ReviewEvaluation Evaluation { get; private set; }
    public int PreviousIntervalDays { get; private set; }
    public int NextIntervalDays { get; private set; }
    public DateTimeOffset PreviousDueAtUtc { get; private set; }
    public DateTimeOffset NextDueAtUtc { get; private set; }
    public DateTimeOffset ReviewedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? SubmissionId { get; private set; }
    public int? ResultReviewCount { get; private set; }

    public static ReviewHistory Create(
        Guid id, Guid reviewItemId, ReviewEvaluation evaluation,
        ReviewSchedule schedule, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Review history id cannot be empty.", nameof(id));
        }

        if (reviewItemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Review item id cannot be empty.", nameof(reviewItemId));
        }

        if (!Enum.IsDefined(evaluation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(evaluation), ReviewErrors.InvalidEvaluation.Message);
        }

        ArgumentNullException.ThrowIfNull(schedule);
        EnsureUtc(schedule.PreviousDueAtUtc, nameof(schedule.PreviousDueAtUtc));
        EnsureUtc(schedule.NextDueAtUtc, nameof(schedule.NextDueAtUtc));
        EnsureUtc(schedule.ReviewedAtUtc, nameof(schedule.ReviewedAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        if (schedule.PreviousIntervalDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schedule), "Previous interval cannot be negative.");
        }

        if (schedule.NextIntervalDays <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(schedule), "Next interval must be greater than zero.");
        }

        return new ReviewHistory(
            id, reviewItemId, evaluation, schedule.PreviousIntervalDays,
            schedule.NextIntervalDays, schedule.PreviousDueAtUtc,
            schedule.NextDueAtUtc, schedule.ReviewedAtUtc, createdAtUtc);
    }

    public static ReviewHistory CreateForSubmission(
        Guid id, Guid userId, Guid submissionId, Guid reviewItemId,
        ReviewEvaluation evaluation, ReviewSchedule schedule,
        int resultReviewCount, DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (submissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Submission id cannot be empty.", nameof(submissionId));
        }

        if (resultReviewCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resultReviewCount),
                "Result review count must be greater than zero.");
        }

        var history = Create(
            id, reviewItemId, evaluation, schedule, createdAtUtc);
        return new ReviewHistory(
            history.Id, history.ReviewItemId, history.Evaluation,
            history.PreviousIntervalDays, history.NextIntervalDays,
            history.PreviousDueAtUtc, history.NextDueAtUtc,
            history.ReviewedAtUtc, history.CreatedAtUtc, userId,
            submissionId, resultReviewCount);
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
