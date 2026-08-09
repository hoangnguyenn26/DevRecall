namespace DevRecall.Domain.Study;

public sealed class StudySessionItem
{
    private StudySessionItem()
    {
    }

    private StudySessionItem(
        Guid id, Guid studySessionId, StudyResourceType resourceType,
        Guid resourceId, int position, string? notes,
        DateTimeOffset createdAtUtc, string? titleSnapshot,
        int plannedDurationMinutes)
    {
        Id = id;
        StudySessionId = studySessionId;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Position = position;
        Status = StudySessionItemStatus.Pending;
        Notes = notes;
        TitleSnapshot = string.IsNullOrWhiteSpace(titleSnapshot)
            ? "Learning resource" : titleSnapshot.Trim();
        PlannedDurationMinutes = Math.Max(0, plannedDurationMinutes);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid StudySessionId { get; private set; }
    public StudyResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public int Position { get; private set; }
    public StudySessionItemStatus Status { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public string TitleSnapshot { get; private set; } = string.Empty;
    public int PlannedDurationMinutes { get; private set; }
    public Guid? CompletionSubmissionId { get; private set; }
    public Guid? EvidenceId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    internal static StudySessionItem Create(
        Guid id, Guid studySessionId, StudyResourceType resourceType,
        Guid resourceId, int position, string? notes,
        DateTimeOffset createdAtUtc, string? titleSnapshot = null,
        int plannedDurationMinutes = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Study session item id cannot be empty.", nameof(id));
        }

        if (studySessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Study session id cannot be empty.", nameof(studySessionId));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Resource id cannot be empty.", nameof(resourceId));
        }

        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position), "Position cannot be negative.");
        }

        EnsureValidResourceType(resourceType);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        return new StudySessionItem(
            id, studySessionId, resourceType, resourceId, position,
            NormalizeNotes(notes, nameof(notes)), createdAtUtc,
            titleSnapshot, plannedDurationMinutes);
    }

    internal void Start(DateTimeOffset startedAtUtc)
    {
        EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        if (Status != StudySessionItemStatus.Pending)
        {
            throw new InvalidOperationException(
                StudySessionErrors.ItemInvalidState.Message);
        }

        Status = StudySessionItemStatus.InProgress;
        StartedAtUtc = startedAtUtc;
        UpdatedAtUtc = startedAtUtc;
    }

    internal void Complete(string? notes, DateTimeOffset completedAtUtc)
    {
        EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        EnsureCanFinish();
        if (StartedAtUtc is not null && completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentException(
                "Completed time cannot be earlier than started time.",
                nameof(completedAtUtc));
        }

        Status = StudySessionItemStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        Notes = NormalizeNotes(notes, nameof(notes));
        UpdatedAtUtc = completedAtUtc;
    }

    internal bool Complete(
        Guid submissionId, Guid? evidenceId, string? notes,
        DateTimeOffset completedAtUtc)
    {
        if (CompletionSubmissionId == submissionId)
        {
            return false;
        }

        Complete(notes, completedAtUtc);
        CompletionSubmissionId = submissionId;
        EvidenceId = evidenceId;
        return true;
    }

    internal void Skip(string? notes, DateTimeOffset skippedAtUtc)
    {
        EnsureUtc(skippedAtUtc, nameof(skippedAtUtc));
        EnsureCanFinish();
        Status = StudySessionItemStatus.Skipped;
        CompletedAtUtc = skippedAtUtc;
        Notes = NormalizeNotes(notes, nameof(notes));
        UpdatedAtUtc = skippedAtUtc;
    }

    internal bool Skip(
        Guid submissionId, string? notes, DateTimeOffset skippedAtUtc)
    {
        if (CompletionSubmissionId == submissionId)
        {
            return false;
        }

        Skip(notes, skippedAtUtc);
        CompletionSubmissionId = submissionId;
        return true;
    }

    internal bool ChangePosition(int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position), "Position cannot be negative.");
        }

        if (Position == position)
        {
            return false;
        }

        Position = position;
        return true;
    }

    private void EnsureCanFinish()
    {
        if (Status is StudySessionItemStatus.Completed
            or StudySessionItemStatus.Skipped)
        {
            throw new InvalidOperationException(
                StudySessionErrors.ItemInvalidState.Message);
        }
    }

    private static void EnsureValidResourceType(StudyResourceType value)
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), StudySessionErrors.InvalidResourceType.Message);
        }
    }

    private static string? NormalizeNotes(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > StudySessionText.ItemNotesMaxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {StudySessionText.ItemNotesMaxLength} characters.",
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
