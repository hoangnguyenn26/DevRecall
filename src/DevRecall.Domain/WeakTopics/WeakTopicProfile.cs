namespace DevRecall.Domain.WeakTopics;

public sealed class WeakTopicProfile
{
    private WeakTopicProfile()
    {
    }

    private WeakTopicProfile(
        Guid id, Guid userId, WeakTopicResourceType resourceType,
        Guid resourceId, WeaknessScoreBreakdown score,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Apply(score);
        Version = 1;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public WeakTopicResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public decimal Score { get; private set; }
    public WeaknessLevel Level { get; private set; }
    public int SignalCount { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CalculatedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static WeakTopicProfile Create(
        Guid id, Guid userId, WeakTopicResourceType resourceType,
        Guid resourceId, WeaknessScoreBreakdown score,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Weak topic profile id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id cannot be empty.", nameof(userId));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Resource id cannot be empty.", nameof(resourceId));
        }

        ArgumentNullException.ThrowIfNull(score);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureResourceType(resourceType);
        return new WeakTopicProfile(
            id, userId, resourceType, resourceId, score, createdAtUtc);
    }

    public bool Recalculate(
        WeaknessScoreBreakdown score, DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(score);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (score.CalculatedAtUtc < CalculatedAtUtc)
        {
            throw new ArgumentException(
                "The recalculation time cannot be older than the current calculation.",
                nameof(score));
        }

        if (Score == score.FinalScore && Level == score.Level
            && SignalCount == score.SignalCount
            && CalculatedAtUtc == score.CalculatedAtUtc)
        {
            return false;
        }

        Apply(score);
        Version++;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    private void Apply(WeaknessScoreBreakdown score)
    {
        Score = score.FinalScore;
        Level = score.Level;
        SignalCount = score.SignalCount;
        CalculatedAtUtc = score.CalculatedAtUtc;
    }

    private static void EnsureResourceType(WeakTopicResourceType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
    }

    private static void EnsureUtc(
        DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"{parameterName} must be in UTC.", parameterName);
        }
    }
}
