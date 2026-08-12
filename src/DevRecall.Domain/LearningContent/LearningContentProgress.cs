namespace DevRecall.Domain.LearningContent;

public enum LearningProgressStatus { InProgress = 1, Completed = 2 }

public sealed class LearningContentProgress
{
    private LearningContentProgress() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid LearningContentId { get; private set; }
    public LearningProgressStatus Status { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static LearningContentProgress Start(Guid id, Guid userId, Guid contentId, DateTimeOffset now) =>
        Create(id, userId, contentId, LearningProgressStatus.InProgress, now, null, now);

    public static LearningContentProgress CompleteDirectly(Guid id, Guid userId, Guid contentId, DateTimeOffset now) =>
        Create(id, userId, contentId, LearningProgressStatus.Completed, null, now, now);

    public bool Complete(int expectedVersion, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (Status == LearningProgressStatus.Completed) return false;
        if (Version != expectedVersion) throw new InvalidOperationException("LEARNING_CONTENT_PROGRESS_CONFLICT");
        Status = LearningProgressStatus.Completed;
        CompletedAtUtc = now;
        UpdatedAtUtc = now;
        Version = checked(Version + 1);
        return true;
    }

    private static LearningContentProgress Create(Guid id, Guid userId, Guid contentId,
        LearningProgressStatus status, DateTimeOffset? started, DateTimeOffset? completed, DateTimeOffset now)
    {
        if (id == Guid.Empty || userId == Guid.Empty || contentId == Guid.Empty) throw new ArgumentException("Ids cannot be empty.");
        EnsureUtc(now);
        return new()
        {
            Id = id,
            UserId = userId,
            LearningContentId = contentId,
            Status = status,
            StartedAtUtc = started,
            CompletedAtUtc = completed,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.");
    }
}

public sealed class LearningContentCompletionEvidence
{
    private LearningContentCompletionEvidence() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid LearningContentId { get; private set; }
    public string TitleSnapshot { get; private set; } = null!;
    public DateTimeOffset CompletedAtUtc { get; private set; }

    public static LearningContentCompletionEvidence Create(Guid id, Guid userId, Guid contentId,
        string titleSnapshot, DateTimeOffset completedAtUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty || contentId == Guid.Empty) throw new ArgumentException("Ids cannot be empty.");
        if (string.IsNullOrWhiteSpace(titleSnapshot)) throw new ArgumentException("Title is required.", nameof(titleSnapshot));
        if (completedAtUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.");
        return new()
        {
            Id = id,
            UserId = userId,
            LearningContentId = contentId,
            TitleSnapshot = titleSnapshot.Trim(),
            CompletedAtUtc = completedAtUtc
        };
    }
}
