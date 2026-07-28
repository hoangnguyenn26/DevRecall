namespace DevRecall.Domain.Study;

public sealed class StudySession
{
    private readonly List<StudySessionItem> _items = [];

    private StudySession()
    {
    }

    private StudySession(
        Guid id, Guid userId, string title, int plannedDurationMinutes,
        string? notes, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Title = title;
        PlannedDurationMinutes = plannedDurationMinutes;
        Notes = notes;
        Status = StudySessionStatus.Planned;
        Version = 1;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public StudySessionStatus Status { get; private set; }
    public int PlannedDurationMinutes { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int? ActualDurationMinutes { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyCollection<StudySessionItem> Items => _items.AsReadOnly();

    public static StudySession Create(
        Guid id, Guid userId, string title, int plannedDurationMinutes,
        string? notes, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Study session id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        return new StudySession(
            id, userId, NormalizeTitle(title),
            ValidatePlannedDuration(plannedDurationMinutes),
            NormalizeNotes(
                notes, nameof(notes), StudySessionText.SessionNotesMaxLength),
            createdAtUtc);
    }

    public bool UpdatePlan(
        string title, int plannedDurationMinutes, string? notes,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsurePlanned();
        var normalizedTitle = NormalizeTitle(title);
        var duration = ValidatePlannedDuration(plannedDurationMinutes);
        var normalizedNotes = NormalizeNotes(
            notes, nameof(notes), StudySessionText.SessionNotesMaxLength);
        if (Title == normalizedTitle
            && PlannedDurationMinutes == duration
            && Notes == normalizedNotes)
        {
            return false;
        }

        Title = normalizedTitle;
        PlannedDurationMinutes = duration;
        Notes = normalizedNotes;
        UpdatedAtUtc = updatedAtUtc;
        IncrementVersion();
        return true;
    }

    public StudySessionItem AddItem(
        Guid itemId, StudyResourceType resourceType, Guid resourceId,
        string? notes, DateTimeOffset createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsurePlanned();
        EnsureValidResourceType(resourceType);
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException(
                "Study session item id cannot be empty.", nameof(itemId));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Resource id cannot be empty.", nameof(resourceId));
        }

        if (_items.Any(item =>
            item.ResourceType == resourceType && item.ResourceId == resourceId))
        {
            throw new InvalidOperationException(
                StudySessionErrors.ItemAlreadyExists.Message);
        }

        var position = _items.Count == 0
            ? 0
            : _items.Max(item => item.Position) + 1;
        var item = StudySessionItem.Create(
            itemId, Id, resourceType, resourceId, position, notes, createdAtUtc);
        _items.Add(item);
        UpdatedAtUtc = createdAtUtc;
        IncrementVersion();
        return item;
    }

    public bool RemoveItem(Guid itemId, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsurePlanned();
        var item = _items.SingleOrDefault(current => current.Id == itemId);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        NormalizePositions();
        UpdatedAtUtc = updatedAtUtc;
        IncrementVersion();
        return true;
    }

    public bool ReorderItems(
        IReadOnlyList<Guid> orderedItemIds, DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(orderedItemIds);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsurePlanned();
        if (orderedItemIds.Count != _items.Count
            || orderedItemIds.Distinct().Count() != orderedItemIds.Count)
        {
            throw new ArgumentException(
                "The reorder request must contain each session item exactly once.",
                nameof(orderedItemIds));
        }

        var existingIds = _items.Select(item => item.Id).ToHashSet();
        if (orderedItemIds.Any(id => !existingIds.Contains(id)))
        {
            throw new ArgumentException(
                "The reorder request contains an unknown session item.",
                nameof(orderedItemIds));
        }

        var changed = false;
        for (var position = 0; position < orderedItemIds.Count; position++)
        {
            var item = _items.Single(
                current => current.Id == orderedItemIds[position]);
            changed |= item.ChangePosition(position);
        }

        if (changed)
        {
            UpdatedAtUtc = updatedAtUtc;
            IncrementVersion();
        }

        return changed;
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        switch (Status)
        {
            case StudySessionStatus.Planned:
                Status = StudySessionStatus.InProgress;
                StartedAtUtc = startedAtUtc;
                UpdatedAtUtc = startedAtUtc;
                IncrementVersion();
                return;
            case StudySessionStatus.InProgress:
                throw new InvalidOperationException(
                    StudySessionErrors.SessionAlreadyStarted.Message);
            case StudySessionStatus.Completed:
                throw new InvalidOperationException(
                    StudySessionErrors.SessionCompleted.Message);
            case StudySessionStatus.Cancelled:
                throw new InvalidOperationException(
                    StudySessionErrors.SessionCancelled.Message);
            default:
                throw new InvalidOperationException(
                    StudySessionErrors.SessionInvalidState.Message);
        }
    }

    public void StartItem(Guid itemId, DateTimeOffset startedAtUtc)
    {
        EnsureInProgress();
        GetItem(itemId).Start(startedAtUtc);
        UpdatedAtUtc = startedAtUtc;
        IncrementVersion();
    }

    public void CompleteItem(
        Guid itemId, string? notes, DateTimeOffset completedAtUtc)
    {
        EnsureInProgress();
        GetItem(itemId).Complete(notes, completedAtUtc);
        UpdatedAtUtc = completedAtUtc;
        IncrementVersion();
    }

    public void SkipItem(
        Guid itemId, string? notes, DateTimeOffset skippedAtUtc)
    {
        EnsureInProgress();
        GetItem(itemId).Skip(notes, skippedAtUtc);
        UpdatedAtUtc = skippedAtUtc;
        IncrementVersion();
    }

    public void Complete(DateTimeOffset completedAtUtc) =>
        Complete(Version, completedAtUtc);

    public StudySessionCompletionSummary Complete(
        int expectedVersion, DateTimeOffset completedAtUtc)
    {
        EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        EnsureExpectedVersion(expectedVersion);
        EnsureInProgress();
        if (StartedAtUtc is null)
        {
            throw new InvalidOperationException(
                StudySessionErrors.SessionNotStarted.Message);
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException(
                "Completed time cannot be earlier than started time.",
                nameof(completedAtUtc));
        }

        Status = StudySessionStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        ActualDurationMinutes = CalculateActualDurationMinutes(
            completedAtUtc - StartedAtUtc.Value);
        UpdatedAtUtc = completedAtUtc;
        IncrementVersion();
        return GetCompletionSummary();
    }

    public bool Cancel(DateTimeOffset cancelledAtUtc) =>
        Cancel(Version, cancelledAtUtc);

    public bool Cancel(int expectedVersion, DateTimeOffset cancelledAtUtc)
    {
        EnsureUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        EnsureExpectedVersion(expectedVersion);
        if (Status == StudySessionStatus.Cancelled)
        {
            return false;
        }

        if (Status == StudySessionStatus.Completed)
        {
            throw new InvalidOperationException(
                StudySessionErrors.SessionCompleted.Message);
        }

        Status = StudySessionStatus.Cancelled;
        UpdatedAtUtc = cancelledAtUtc;
        IncrementVersion();
        return true;
    }

    public StudySessionCompletionSummary GetCompletionSummary() =>
        new(
            _items.Count,
            _items.Count(item => item.Status == StudySessionItemStatus.Pending),
            _items.Count(item =>
                item.Status == StudySessionItemStatus.InProgress),
            _items.Count(item =>
                item.Status == StudySessionItemStatus.Completed),
            _items.Count(item => item.Status == StudySessionItemStatus.Skipped),
            CountCompleted(StudyResourceType.KnowledgeNode),
            CountCompleted(StudyResourceType.InterviewQuestion),
            CountCompleted(StudyResourceType.DsaProblem),
            CountCompleted(StudyResourceType.ReviewItem));

    private StudySessionItem GetItem(Guid itemId) =>
        _items.SingleOrDefault(item => item.Id == itemId)
        ?? throw new KeyNotFoundException(StudySessionErrors.ItemNotFound.Message);

    private void EnsurePlanned()
    {
        if (Status == StudySessionStatus.Planned)
        {
            return;
        }

        throw new InvalidOperationException(Status switch
        {
            StudySessionStatus.InProgress =>
                StudySessionErrors.SessionAlreadyStarted.Message,
            StudySessionStatus.Completed =>
                StudySessionErrors.SessionCompleted.Message,
            StudySessionStatus.Cancelled =>
                StudySessionErrors.SessionCancelled.Message,
            _ => StudySessionErrors.SessionInvalidState.Message
        });
    }

    private void EnsureInProgress()
    {
        if (Status == StudySessionStatus.InProgress)
        {
            return;
        }

        throw new InvalidOperationException(Status switch
        {
            StudySessionStatus.Planned =>
                StudySessionErrors.SessionNotStarted.Message,
            StudySessionStatus.Completed =>
                StudySessionErrors.SessionCompleted.Message,
            StudySessionStatus.Cancelled =>
                StudySessionErrors.SessionCancelled.Message,
            _ => StudySessionErrors.SessionInvalidState.Message
        });
    }

    private void NormalizePositions()
    {
        var ordered = _items.OrderBy(item => item.Position).ToList();
        for (var position = 0; position < ordered.Count; position++)
        {
            ordered[position].ChangePosition(position);
        }
    }

    private static int CalculateActualDurationMinutes(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return elapsed.TotalMinutes >= int.MaxValue
            ? int.MaxValue
            : (int)Math.Floor(elapsed.TotalMinutes);
    }

    private int CountCompleted(StudyResourceType resourceType) =>
        _items.Count(item =>
            item.ResourceType == resourceType
            && item.Status == StudySessionItemStatus.Completed);

    private void EnsureExpectedVersion(int expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedVersion),
                "Expected version must be greater than zero.");
        }

        if (Version != expectedVersion)
        {
            throw new StudySessionDomainException(
                StudySessionErrors.Conflict);
        }
    }

    private void IncrementVersion() => Version = checked(Version + 1);

    private static int ValidatePlannedDuration(int value)
    {
        if (value is < 0 or > StudySessionText.MaximumPlannedDurationMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Planned duration must be between 0 and {StudySessionText.MaximumPlannedDurationMinutes} minutes.");
        }

        return value;
    }

    private static void EnsureValidResourceType(StudyResourceType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), StudySessionErrors.InvalidResourceType.Message);
        }
    }

    private static string NormalizeTitle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Study session title is required.", nameof(value));
        }

        var normalized = string.Join(
            ' ', value.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length > StudySessionText.TitleMaxLength)
        {
            throw new ArgumentException(
                $"Study session title cannot exceed {StudySessionText.TitleMaxLength} characters.",
                nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeNotes(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
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
