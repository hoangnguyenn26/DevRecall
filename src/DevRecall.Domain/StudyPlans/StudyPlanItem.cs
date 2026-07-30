namespace DevRecall.Domain.StudyPlans;

public sealed class StudyPlanItem
{
    private StudyPlanItem()
    {
    }

    internal StudyPlanItem(
        Guid id, Guid? sourceRecommendationId, StudyPlanSourceType sourceType,
        StudyPlanResourceType resourceType, Guid resourceId,
        int plannedDurationMinutes, int position, DateTimeOffset createdAtUtc)
    {
        if (!Enum.IsDefined(sourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceType));
        }

        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceType));
        }

        if (sourceType == StudyPlanSourceType.Recommendation
            && sourceRecommendationId is null)
        {
            throw new ArgumentException(
                "A recommendation source requires its recommendation id.",
                nameof(sourceRecommendationId));
        }

        Id = id;
        SourceRecommendationId = sourceRecommendationId;
        SourceType = sourceType;
        ResourceType = resourceType;
        ResourceId = resourceId;
        PlannedDurationMinutes = plannedDurationMinutes;
        Position = position;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid StudyPlanId { get; private set; }
    public Guid? SourceRecommendationId { get; private set; }
    public StudyPlanSourceType SourceType { get; private set; }
    public StudyPlanResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public int PlannedDurationMinutes { get; private set; }
    public int Position { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    internal void UpdateDuration(
        int plannedDurationMinutes, DateTimeOffset updatedAtUtc)
    {
        PlannedDurationMinutes = plannedDurationMinutes;
        UpdatedAtUtc = updatedAtUtc;
    }

    internal void SetPosition(int position, DateTimeOffset updatedAtUtc)
    {
        Position = position;
        UpdatedAtUtc = updatedAtUtc;
    }
}
