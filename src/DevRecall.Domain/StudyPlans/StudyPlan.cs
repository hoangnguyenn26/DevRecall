namespace DevRecall.Domain.StudyPlans;

public sealed record InitialStudyPlanItem(
    Guid ItemId,
    Guid RecommendationId,
    StudyPlanResourceType ResourceType,
    Guid ResourceId,
    int PlannedDurationMinutes);

public sealed class StudyPlan
{
    private readonly List<StudyPlanItem> _items = [];

    private StudyPlan()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public StudyPlanStatus Status { get; private set; }
    public DateTimeOffset GeneratedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ReadyAtUtc { get; private set; }
    public DateTimeOffset? ConvertedAtUtc { get; private set; }
    public Guid? ConvertedStudySessionId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyCollection<StudyPlanItem> Items => _items.AsReadOnly();
    public int TotalPlannedDurationMinutes =>
        _items.Sum(item => item.PlannedDurationMinutes);

    public static StudyPlan Create(
        Guid id, Guid userId, string title, DateTimeOffset generatedAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Study plan id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        if (expiresAtUtc is not null)
        {
            EnsureUtc(expiresAtUtc.Value, nameof(expiresAtUtc));
            if (expiresAtUtc <= generatedAtUtc)
            {
                throw new ArgumentException(
                    "Plan expiration must be later than generation time.",
                    nameof(expiresAtUtc));
            }
        }

        return new StudyPlan
        {
            Id = id,
            UserId = userId,
            Title = NormalizeTitle(title),
            Status = StudyPlanStatus.Draft,
            GeneratedAtUtc = generatedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = generatedAtUtc,
            UpdatedAtUtc = generatedAtUtc,
            Version = 1
        };
    }

    public static StudyPlan CreateFromRecommendations(
        Guid id, Guid userId, string title,
        IReadOnlyCollection<InitialStudyPlanItem> items,
        DateTimeOffset generatedAtUtc, DateTimeOffset? expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.EmptyPlan);
        }

        var plan = Create(
            id, userId, title, generatedAtUtc, expiresAtUtc);
        foreach (var item in items)
        {
            plan.AddInitialRecommendationItem(item, generatedAtUtc);
        }

        plan.Version = 1;
        plan.UpdatedAtUtc = generatedAtUtc;
        return plan;
    }

    public StudyPlanItem AddRecommendationItem(
        Guid itemId, Guid recommendationId, StudyPlanResourceType resourceType,
        Guid resourceId, int plannedDurationMinutes, DateTimeOffset addedAtUtc)
    {
        EnsureDraft();
        EnsureUtc(addedAtUtc, nameof(addedAtUtc));
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Study plan item id cannot be empty.", nameof(itemId));
        }

        if (recommendationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recommendation id cannot be empty.", nameof(recommendationId));
        }

        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceType));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException("Resource id cannot be empty.", nameof(resourceId));
        }

        EnsureDuration(plannedDurationMinutes);
        EnsureCapacity(plannedDurationMinutes);
        EnsureResourceNotDuplicated(resourceType, resourceId);
        var item = new StudyPlanItem(
            itemId, recommendationId, StudyPlanSourceType.Recommendation,
            resourceType, resourceId, plannedDurationMinutes, _items.Count + 1,
            addedAtUtc);
        _items.Add(item);
        Touch(addedAtUtc);
        return item;
    }

    public bool UpdateItemDuration(
        Guid itemId, int plannedDurationMinutes, DateTimeOffset updatedAtUtc)
    {
        EnsureDraft();
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureDuration(plannedDurationMinutes);
        var item = GetItem(itemId);
        if (item.PlannedDurationMinutes == plannedDurationMinutes)
        {
            return false;
        }

        var newTotal = TotalPlannedDurationMinutes
            - item.PlannedDurationMinutes + plannedDurationMinutes;
        if (newTotal > StudyPlanDefaults.MaximumTotalDurationMinutes)
        {
            throw new StudyPlanDomainException(
                StudyPlanErrors.DurationLimitExceeded);
        }

        item.UpdateDuration(plannedDurationMinutes, updatedAtUtc);
        Touch(updatedAtUtc);
        return true;
    }

    public bool RemoveItem(Guid itemId, DateTimeOffset removedAtUtc)
    {
        EnsureDraft();
        EnsureUtc(removedAtUtc, nameof(removedAtUtc));
        var item = _items.SingleOrDefault(item => item.Id == itemId);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        NormalizePositions(removedAtUtc);
        Touch(removedAtUtc);
        return true;
    }

    public bool ReorderItems(
        IReadOnlyList<Guid> orderedItemIds, DateTimeOffset reorderedAtUtc)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(orderedItemIds);
        EnsureUtc(reorderedAtUtc, nameof(reorderedAtUtc));
        if (orderedItemIds.Count != _items.Count
            || orderedItemIds.Count != orderedItemIds.Distinct().Count())
        {
            throw new StudyPlanDomainException(StudyPlanErrors.InvalidItemOrder);
        }

        var currentIds = _items.Select(item => item.Id).ToHashSet();
        if (orderedItemIds.Any(id => !currentIds.Contains(id)))
        {
            throw new StudyPlanDomainException(StudyPlanErrors.InvalidItemOrder);
        }

        if (_items.OrderBy(item => item.Position).Select(item => item.Id)
            .SequenceEqual(orderedItemIds))
        {
            return false;
        }

        var itemById = _items.ToDictionary(item => item.Id);
        for (var index = 0; index < orderedItemIds.Count; index++)
        {
            itemById[orderedItemIds[index]].SetPosition(index + 1, reorderedAtUtc);
        }

        Touch(reorderedAtUtc);
        return true;
    }

    public bool MarkReady(int expectedVersion, DateTimeOffset readyAtUtc)
    {
        EnsureExpectedVersion(expectedVersion);
        EnsureUtc(readyAtUtc, nameof(readyAtUtc));
        if (Status == StudyPlanStatus.Ready)
        {
            return false;
        }

        EnsureDraft();
        if (_items.Count == 0)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.EmptyPlan);
        }

        Status = StudyPlanStatus.Ready;
        ReadyAtUtc = readyAtUtc;
        Touch(readyAtUtc);
        return true;
    }

    public bool Cancel(int expectedVersion, DateTimeOffset cancelledAtUtc)
    {
        EnsureExpectedVersion(expectedVersion);
        EnsureUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        if (Status == StudyPlanStatus.Cancelled)
        {
            return false;
        }

        if (Status == StudyPlanStatus.Converted)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.AlreadyConverted);
        }

        Status = StudyPlanStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        Touch(cancelledAtUtc);
        return true;
    }

    public bool MarkConverted(
        int expectedVersion, Guid studySessionId, DateTimeOffset convertedAtUtc)
    {
        EnsureExpectedVersion(expectedVersion);
        EnsureUtc(convertedAtUtc, nameof(convertedAtUtc));
        if (studySessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Study session id cannot be empty.", nameof(studySessionId));
        }

        if (Status == StudyPlanStatus.Converted)
        {
            return false;
        }

        if (Status != StudyPlanStatus.Ready)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.NotReady);
        }

        Status = StudyPlanStatus.Converted;
        ConvertedStudySessionId = studySessionId;
        ConvertedAtUtc = convertedAtUtc;
        Touch(convertedAtUtc);
        return true;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new StudyPlanDomainException(StudyPlanErrors.TitleRequired);
        }

        var normalized = title.Trim();
        if (normalized.Length > StudyPlanDefaults.MaximumTitleLength)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.TitleTooLong);
        }

        return normalized;
    }

    private void AddInitialRecommendationItem(
        InitialStudyPlanItem item, DateTimeOffset addedAtUtc)
    {
        if (item.ItemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Study plan item id cannot be empty.", nameof(item));
        }

        if (item.RecommendationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Recommendation id cannot be empty.", nameof(item));
        }

        if (!Enum.IsDefined(item.ResourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(item));
        }

        if (item.ResourceId == Guid.Empty)
        {
            throw new ArgumentException("Resource id cannot be empty.", nameof(item));
        }

        EnsureDuration(item.PlannedDurationMinutes);
        EnsureCapacity(item.PlannedDurationMinutes);
        EnsureResourceNotDuplicated(item.ResourceType, item.ResourceId);
        _items.Add(new StudyPlanItem(
            item.ItemId, item.RecommendationId,
            StudyPlanSourceType.Recommendation, item.ResourceType,
            item.ResourceId, item.PlannedDurationMinutes, _items.Count + 1,
            addedAtUtc));
    }

    private static void EnsureDuration(int duration)
    {
        if (duration is < StudyPlanDefaults.MinimumItemDurationMinutes
            or > StudyPlanDefaults.MaximumItemDurationMinutes)
        {
            throw new StudyPlanDomainException(
                StudyPlanErrors.InvalidItemDuration);
        }
    }

    private void EnsureCapacity(int additionalMinutes)
    {
        if (_items.Count >= StudyPlanDefaults.MaximumItems)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.ItemLimitReached);
        }

        if (TotalPlannedDurationMinutes + additionalMinutes
            > StudyPlanDefaults.MaximumTotalDurationMinutes)
        {
            throw new StudyPlanDomainException(
                StudyPlanErrors.DurationLimitExceeded);
        }
    }

    private void EnsureResourceNotDuplicated(
        StudyPlanResourceType resourceType, Guid resourceId)
    {
        if (_items.Any(item => item.ResourceType == resourceType
            && item.ResourceId == resourceId))
        {
            throw new StudyPlanDomainException(StudyPlanErrors.DuplicateResource);
        }
    }

    private StudyPlanItem GetItem(Guid itemId)
    {
        var item = _items.SingleOrDefault(item => item.Id == itemId);
        return item
            ?? throw new StudyPlanDomainException(StudyPlanErrors.ItemNotFound);
    }

    private void NormalizePositions(DateTimeOffset updatedAtUtc)
    {
        var ordered = _items.OrderBy(item => item.Position).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            if (ordered[index].Position != index + 1)
            {
                ordered[index].SetPosition(index + 1, updatedAtUtc);
            }
        }
    }

    private void EnsureDraft()
    {
        if (Status != StudyPlanStatus.Draft)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.NotDraft);
        }
    }

    private void EnsureExpectedVersion(int expectedVersion)
    {
        if (expectedVersion != Version)
        {
            throw new StudyPlanDomainException(StudyPlanErrors.Conflict);
        }
    }

    private void Touch(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
        Version = checked(Version + 1);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
