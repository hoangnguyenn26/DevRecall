namespace DevRecall.Domain.Dsa.Attempts;

public sealed class DsaPracticeSubmission
{
    private DsaPracticeSubmission() { }

    private DsaPracticeSubmission(
        Guid id, Guid userId, Guid dsaProblemId, Guid dsaAttemptId,
        Guid submissionId, string problemTitleSnapshot,
        string difficultySnapshot, DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        Id = id;
        UserId = userId;
        DsaProblemId = dsaProblemId;
        DsaAttemptId = dsaAttemptId;
        SubmissionId = submissionId;
        ProblemTitleSnapshot = problemTitleSnapshot;
        DifficultySnapshot = difficultySnapshot;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        DurationSeconds = checked((int)Math.Clamp((completedAtUtc - startedAtUtc).TotalSeconds, 0, 86_400));
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DsaProblemId { get; private set; }
    public Guid DsaAttemptId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string ProblemTitleSnapshot { get; private set; } = null!;
    public string DifficultySnapshot { get; private set; } = null!;
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }

    public static DsaPracticeSubmission Create(
        Guid id, Guid userId, Guid problemId, Guid attemptId, Guid submissionId,
        string title, string difficulty, DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty || problemId == Guid.Empty || attemptId == Guid.Empty || submissionId == Guid.Empty)
            throw new ArgumentException("Practice submission identifiers cannot be empty.");
        if (startedAtUtc.Offset != TimeSpan.Zero || completedAtUtc.Offset != TimeSpan.Zero || startedAtUtc > completedAtUtc)
            throw new ArgumentException("Practice timestamps must be valid UTC values.");
        return new DsaPracticeSubmission(
            id, userId, problemId, attemptId, submissionId, title.Trim(),
            difficulty, startedAtUtc, completedAtUtc);
    }
}
