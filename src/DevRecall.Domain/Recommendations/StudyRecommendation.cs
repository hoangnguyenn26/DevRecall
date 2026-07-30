namespace DevRecall.Domain.Recommendations;

public sealed class StudyRecommendation
{
    private StudyRecommendation()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public RecommendationResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public RecommendationType Type { get; private set; }
    public RecommendationPriority Priority { get; private set; }
    public decimal PriorityScore { get; private set; }
    public RecommendationStatus Status { get; private set; }
    public RecommendationReason Reason { get; private set; } = null!;
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? DismissedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ExpiredAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    public static StudyRecommendation Create(
        Guid id, Guid userId, RecommendationResourceType resourceType,
        Guid resourceId, RecommendationType type,
        RecommendationPriority priority, RecommendationReason reason,
        DateTimeOffset generatedAtUtc, DateTimeOffset? expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Recommendation id cannot be empty.", nameof(id));
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

        ArgumentNullException.ThrowIfNull(reason);
        EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        ValidateExpiration(generatedAtUtc, expiresAtUtc);
        EnsureRecommendationMatchesResource(resourceType, type);
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        return new()
        {
            Id = id,
            UserId = userId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Type = type,
            Priority = priority,
            PriorityScore = reason.WeaknessScore,
            Status = RecommendationStatus.Active,
            Reason = reason,
            GeneratedAtUtc = generatedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = generatedAtUtc,
            UpdatedAtUtc = generatedAtUtc,
            Version = 1
        };
    }

    public bool Refresh(
        RecommendationPriority priority, RecommendationReason reason,
        DateTimeOffset generatedAtUtc, DateTimeOffset? expiresAtUtc)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(reason);
        EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        ValidateExpiration(generatedAtUtc, expiresAtUtc);
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        if (reason.WeaknessCalculatedAtUtc < Reason.WeaknessCalculatedAtUtc)
        {
            throw new ArgumentException(
                "Recommendation cannot be refreshed from an older weakness calculation.",
                nameof(reason));
        }

        if (Priority == priority && PriorityScore == reason.WeaknessScore
            && Reason == reason)
        {
            return false;
        }

        Priority = priority;
        PriorityScore = reason.WeaknessScore;
        Reason = reason;
        GeneratedAtUtc = generatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = generatedAtUtc;
        Version = checked(Version + 1);
        return true;
    }

    public bool Dismiss(int expectedVersion, DateTimeOffset dismissedAtUtc)
    {
        EnsureExpectedVersion(expectedVersion);
        EnsureUtc(dismissedAtUtc, nameof(dismissedAtUtc));
        if (Status == RecommendationStatus.Dismissed)
        {
            return false;
        }

        EnsureActive();
        Status = RecommendationStatus.Dismissed;
        DismissedAtUtc = dismissedAtUtc;
        UpdatedAtUtc = dismissedAtUtc;
        Version = checked(Version + 1);
        return true;
    }

    public bool Complete(int expectedVersion, DateTimeOffset completedAtUtc)
    {
        EnsureExpectedVersion(expectedVersion);
        EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        if (Status == RecommendationStatus.Completed)
        {
            return false;
        }

        EnsureActive();
        Status = RecommendationStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
        Version = checked(Version + 1);
        return true;
    }

    public bool Expire(DateTimeOffset expiredAtUtc)
    {
        EnsureUtc(expiredAtUtc, nameof(expiredAtUtc));
        if (Status != RecommendationStatus.Active
            || ExpiresAtUtc is null || expiredAtUtc < ExpiresAtUtc)
        {
            return false;
        }

        Status = RecommendationStatus.Expired;
        ExpiredAtUtc = expiredAtUtc;
        UpdatedAtUtc = expiredAtUtc;
        Version = checked(Version + 1);
        return true;
    }

    private void EnsureActive()
    {
        if (Status != RecommendationStatus.Active)
        {
            throw new RecommendationDomainException(RecommendationErrors.NotActive);
        }
    }

    private void EnsureExpectedVersion(int expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw new RecommendationDomainException(
                RecommendationErrors.InvalidVersion);
        }

        if (Version != expectedVersion)
        {
            throw new RecommendationDomainException(RecommendationErrors.Conflict);
        }
    }

    private static void EnsureRecommendationMatchesResource(
        RecommendationResourceType resourceType, RecommendationType type)
    {
        var valid = (resourceType, type) switch
        {
            (RecommendationResourceType.KnowledgeNode,
                RecommendationType.ReviewKnowledge) => true,
            (RecommendationResourceType.InterviewQuestion,
                RecommendationType.PracticeInterview) => true,
            (RecommendationResourceType.DsaProblem,
                RecommendationType.RetryDsaProblem) => true,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException(
                "The recommendation type does not match the resource type.");
        }
    }

    private static void ValidateExpiration(
        DateTimeOffset generatedAtUtc, DateTimeOffset? expiresAtUtc)
    {
        if (expiresAtUtc is null)
        {
            return;
        }

        EnsureUtc(expiresAtUtc.Value, nameof(expiresAtUtc));
        if (expiresAtUtc <= generatedAtUtc)
        {
            throw new ArgumentException(
                "Expiration time must be later than generation time.",
                nameof(expiresAtUtc));
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
