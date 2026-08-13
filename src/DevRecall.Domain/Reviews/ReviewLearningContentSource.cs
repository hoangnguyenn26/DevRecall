namespace DevRecall.Domain.Reviews;

public sealed class ReviewLearningContentSource
{
    private ReviewLearningContentSource() { }
    private ReviewLearningContentSource(Guid reviewItemId, Guid userId, Guid learningContentId,
        Guid candidateId, string candidateKey, string sourceTitleSnapshot, string promptSnapshot,
        string answerSnapshot, DateTimeOffset createdAtUtc)
    {
        ReviewItemId = reviewItemId;
        UserId = userId;
        LearningContentId = learningContentId;
        CandidateId = candidateId;
        CandidateKey = candidateKey;
        SourceTitleSnapshot = sourceTitleSnapshot;
        PromptSnapshot = promptSnapshot;
        AnswerSnapshot = answerSnapshot;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ReviewItemId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid LearningContentId { get; private set; }
    public Guid CandidateId { get; private set; }
    public string CandidateKey { get; private set; } = null!;
    public string SourceTitleSnapshot { get; private set; } = null!;
    public string PromptSnapshot { get; private set; } = null!;
    public string AnswerSnapshot { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ReviewLearningContentSource Create(Guid reviewItemId, Guid userId,
        Guid learningContentId, Guid candidateId, string candidateKey, string sourceTitleSnapshot,
        string promptSnapshot, string answerSnapshot, DateTimeOffset createdAtUtc)
    {
        if (reviewItemId == Guid.Empty || userId == Guid.Empty || learningContentId == Guid.Empty
            || candidateId == Guid.Empty) throw new ArgumentException("Review source identifiers cannot be empty.");
        return new(reviewItemId, userId, learningContentId, candidateId, candidateKey,
            sourceTitleSnapshot, promptSnapshot, answerSnapshot, createdAtUtc);
    }
}

public sealed class LearningContentReviewSubmission
{
    private LearningContentReviewSubmission() { }
    private LearningContentReviewSubmission(Guid userId, Guid submissionId, Guid learningContentId,
        int createdCount, int existingCount, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        SubmissionId = submissionId;
        LearningContentId = learningContentId;
        CreatedCount = createdCount;
        ExistingCount = existingCount;
        CreatedAtUtc = createdAtUtc;
    }
    public Guid UserId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid LearningContentId { get; private set; }
    public int CreatedCount { get; private set; }
    public int ExistingCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static LearningContentReviewSubmission Create(Guid userId, Guid submissionId,
        Guid learningContentId, int createdCount, int existingCount, DateTimeOffset createdAtUtc) =>
        new(userId, submissionId, learningContentId, createdCount, existingCount, createdAtUtc);
}

public sealed class LearningContentReviewSubmissionItem
{
    private LearningContentReviewSubmissionItem() { }
    private LearningContentReviewSubmissionItem(Guid userId, Guid submissionId, Guid reviewItemId,
        string candidateKey, bool wasCreated)
    {
        UserId = userId;
        SubmissionId = submissionId;
        ReviewItemId = reviewItemId;
        CandidateKey = candidateKey;
        WasCreated = wasCreated;
    }
    public Guid UserId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid ReviewItemId { get; private set; }
    public string CandidateKey { get; private set; } = null!;
    public bool WasCreated { get; private set; }

    public static LearningContentReviewSubmissionItem Create(Guid userId, Guid submissionId,
        Guid reviewItemId, string candidateKey, bool wasCreated) =>
        new(userId, submissionId, reviewItemId, candidateKey, wasCreated);
}
