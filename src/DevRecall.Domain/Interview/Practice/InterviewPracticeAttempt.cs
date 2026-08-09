using DevRecall.Domain.Interview.Answers;

namespace DevRecall.Domain.Interview.Practice;

public sealed class InterviewPracticeAttempt
{
    private readonly List<InterviewPracticeFollowUpAttempt> _followUps = [];

    private InterviewPracticeAttempt() { }

    private InterviewPracticeAttempt(
        Guid id, Guid userId, Guid questionId, Guid submissionId,
        string questionSnapshot, int questionVersion, string answerSnapshot,
        InterviewSelfRating selfRating, DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc, int followUpsSkipped)
    {
        Id = id;
        UserId = userId;
        QuestionId = questionId;
        SubmissionId = submissionId;
        QuestionSnapshot = questionSnapshot;
        QuestionVersion = questionVersion;
        AnswerSnapshot = answerSnapshot;
        SelfRating = selfRating;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        DurationSeconds = checked((int)Math.Clamp(
            (completedAtUtc - startedAtUtc).TotalSeconds, 0, 86_400));
        FollowUpsSkipped = followUpsSkipped;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string QuestionSnapshot { get; private set; } = null!;
    public int QuestionVersion { get; private set; }
    public string AnswerSnapshot { get; private set; } = null!;
    public InterviewSelfRating SelfRating { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public int FollowUpsSkipped { get; private set; }
    public IReadOnlyCollection<InterviewPracticeFollowUpAttempt> FollowUps => _followUps.AsReadOnly();

    public static InterviewPracticeAttempt Create(
        Guid id, Guid userId, Guid questionId, Guid submissionId,
        string questionSnapshot, int questionVersion, string answer,
        InterviewSelfRating selfRating, DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        IEnumerable<(Guid FollowUpId, string Prompt, string Answer)> followUps,
        int totalFollowUps)
    {
        if (id == Guid.Empty || userId == Guid.Empty || questionId == Guid.Empty || submissionId == Guid.Empty)
            throw new ArgumentException("Practice attempt identifiers cannot be empty.");
        if (!Enum.IsDefined(selfRating)) throw new ArgumentOutOfRangeException(nameof(selfRating));
        if (startedAtUtc.Offset != TimeSpan.Zero || completedAtUtc.Offset != TimeSpan.Zero || startedAtUtc > completedAtUtc)
            throw new ArgumentException("Practice timestamps must be valid UTC values.");

        var attempt = new InterviewPracticeAttempt(
            id, userId, questionId, submissionId,
            InterviewAnswerContent.Normalize(questionSnapshot),
            questionVersion, InterviewAnswerContent.Normalize(answer),
            selfRating, startedAtUtc, completedAtUtc, 0);
        foreach (var followUp in followUps)
            attempt._followUps.Add(InterviewPracticeFollowUpAttempt.Create(
                Guid.NewGuid(), id, followUp.FollowUpId, followUp.Prompt, followUp.Answer));
        attempt.FollowUpsSkipped = Math.Max(0, totalFollowUps - attempt._followUps.Count);
        return attempt;
    }
}

public sealed class InterviewPracticeFollowUpAttempt
{
    private InterviewPracticeFollowUpAttempt() { }

    private InterviewPracticeFollowUpAttempt(
        Guid id, Guid attemptId, Guid followUpId, string questionSnapshot,
        string answerSnapshot)
    {
        Id = id;
        InterviewPracticeAttemptId = attemptId;
        FollowUpId = followUpId;
        QuestionSnapshot = questionSnapshot;
        AnswerSnapshot = answerSnapshot;
    }

    public Guid Id { get; private set; }
    public Guid InterviewPracticeAttemptId { get; private set; }
    public Guid FollowUpId { get; private set; }
    public string QuestionSnapshot { get; private set; } = null!;
    public string AnswerSnapshot { get; private set; } = null!;

    internal static InterviewPracticeFollowUpAttempt Create(
        Guid id, Guid attemptId, Guid followUpId, string question, string answer) =>
        new(id, attemptId, followUpId,
            InterviewAnswerContent.Normalize(question),
            InterviewAnswerContent.Normalize(answer));
}
