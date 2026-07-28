namespace DevRecall.Domain.Reviews;

public sealed class ReviewItem
{
    private ReviewItem()
    {
    }

    private ReviewItem(
        Guid id, Guid userId, ReviewResourceType resourceType, Guid resourceId,
        DateTimeOffset dueAtUtc, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Status = ReviewItemStatus.Active;
        DueAtUtc = dueAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ReviewResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public ReviewItemStatus Status { get; private set; }
    public DateTimeOffset DueAtUtc { get; private set; }
    public DateTimeOffset? LastReviewedAtUtc { get; private set; }
    public int IntervalDays { get; private set; }
    public int ReviewCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ReviewItem Create(
        Guid id, Guid userId, ReviewResourceType resourceType, Guid resourceId,
        DateTimeOffset dueAtUtc, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Review item id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Resource id cannot be empty.", nameof(resourceId));
        }

        EnsureValidResourceType(resourceType);
        EnsureUtc(dueAtUtc, nameof(dueAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new ReviewItem(
            id, userId, resourceType, resourceId, dueAtUtc, createdAtUtc);
    }

    public ReviewSchedule Evaluate(
        ReviewEvaluation evaluation, int expectedReviewCount,
        DateTimeOffset reviewedAtUtc)
    {
        EnsureActive();
        if (expectedReviewCount != ReviewCount)
        {
            throw new ReviewScheduleConflictException();
        }

        var schedule = ReviewScheduleCalculator.Calculate(
            IntervalDays, DueAtUtc, evaluation, reviewedAtUtc);

        IntervalDays = schedule.NextIntervalDays;
        DueAtUtc = schedule.NextDueAtUtc;
        LastReviewedAtUtc = schedule.ReviewedAtUtc;
        ReviewCount = checked(ReviewCount + 1);
        UpdatedAtUtc = schedule.ReviewedAtUtc;
        return schedule;
    }

    public bool Archive(DateTimeOffset archivedAtUtc)
    {
        EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        if (Status == ReviewItemStatus.Archived)
        {
            return false;
        }

        Status = ReviewItemStatus.Archived;
        UpdatedAtUtc = archivedAtUtc;
        return true;
    }

    private void EnsureActive()
    {
        if (Status == ReviewItemStatus.Archived)
        {
            throw new ReviewItemArchivedException();
        }
    }

    private static void EnsureValidResourceType(ReviewResourceType resourceType)
    {
        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(resourceType), ReviewErrors.InvalidResourceType.Message);
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
