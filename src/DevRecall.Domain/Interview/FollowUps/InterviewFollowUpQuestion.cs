namespace DevRecall.Domain.Interview.FollowUps;

public sealed class InterviewFollowUpQuestion
{
    private InterviewFollowUpQuestion()
    {
    }

    private InterviewFollowUpQuestion(
        Guid id, Guid interviewQuestionId, string prompt, int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        InterviewQuestionId = interviewQuestionId;
        Prompt = prompt;
        SortOrder = sortOrder;
        Status = InterviewFollowUpQuestionStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid InterviewQuestionId { get; private set; }
    public string Prompt { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public InterviewFollowUpQuestionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static InterviewFollowUpQuestion Create(
        Guid id, Guid interviewQuestionId, string prompt, int sortOrder,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Follow-up id cannot be empty.", nameof(id));
        }

        if (interviewQuestionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Interview question id cannot be empty.",
                nameof(interviewQuestionId));
        }

        EnsureValidOrder(sortOrder);
        return new InterviewFollowUpQuestion(
            id, interviewQuestionId, InterviewFollowUpPrompt.Normalize(prompt),
            sortOrder, createdAtUtc);
    }

    public bool UpdatePrompt(string prompt, DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        var normalized = InterviewFollowUpPrompt.Normalize(prompt);

        if (string.Equals(Prompt, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        Prompt = normalized;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool ChangeSortOrder(int sortOrder, DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        EnsureValidOrder(sortOrder);

        if (SortOrder == sortOrder)
        {
            return false;
        }

        SortOrder = sortOrder;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status == InterviewFollowUpQuestionStatus.Archived)
        {
            return false;
        }

        Status = InterviewFollowUpQuestionStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    private void EnsureActive()
    {
        if (Status == InterviewFollowUpQuestionStatus.Archived)
        {
            throw new InvalidOperationException(
                InterviewFollowUpQuestionErrors.Archived.Message);
        }
    }

    private static void EnsureValidOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                InterviewFollowUpQuestionErrors.InvalidOrder.Message);
        }
    }
}
