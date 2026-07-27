namespace DevRecall.Domain.Interview.Answers;

public sealed class InterviewAnswerVersion
{
    private InterviewAnswerVersion()
    {
    }

    private InterviewAnswerVersion(
        Guid id, Guid interviewQuestionId, int versionNumber, string content,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        InterviewQuestionId = interviewQuestionId;
        VersionNumber = versionNumber;
        Content = content;
        Status = InterviewAnswerVersionStatus.Draft;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid InterviewQuestionId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Content { get; private set; } = null!;
    public InterviewAnswerVersionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public static InterviewAnswerVersion CreateDraft(
        Guid id, Guid interviewQuestionId, int versionNumber, string content,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Answer version id cannot be empty.",
                nameof(id));
        }

        if (interviewQuestionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Interview question id cannot be empty.",
                nameof(interviewQuestionId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(versionNumber),
                InterviewAnswerVersionErrors.InvalidVersionNumber.Message);
        }

        return new InterviewAnswerVersion(
            id, interviewQuestionId, versionNumber,
            InterviewAnswerContent.Normalize(content), createdAtUtc);
    }

    public bool UpdateContent(string content, DateTimeOffset updatedAtUtc)
    {
        EnsureDraft();
        var normalizedContent = InterviewAnswerContent.Normalize(content);

        if (string.Equals(Content, normalizedContent, StringComparison.Ordinal))
        {
            return false;
        }

        Content = normalizedContent;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool Publish(DateTimeOffset publishedAtUtc)
    {
        if (Status == InterviewAnswerVersionStatus.Published)
        {
            return false;
        }

        Status = InterviewAnswerVersionStatus.Published;
        PublishedAtUtc = publishedAtUtc;
        UpdatedAtUtc = publishedAtUtc;
        return true;
    }

    private void EnsureDraft()
    {
        if (Status == InterviewAnswerVersionStatus.Published)
        {
            throw new InvalidOperationException(
                InterviewAnswerVersionErrors.Published.Message);
        }
    }
}
