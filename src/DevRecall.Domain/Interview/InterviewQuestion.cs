namespace DevRecall.Domain.Interview;

public sealed class InterviewQuestion
{
    private InterviewQuestion()
    {
    }

    private InterviewQuestion(
        Guid id, Guid userId, string title, string question, string topic,
        InterviewQuestionDifficulty difficulty, string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Question = question;
        Topic = topic;
        Difficulty = difficulty;
        Notes = notes;
        Status = InterviewQuestionStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Question { get; private set; } = null!;
    public string Topic { get; private set; } = null!;
    public InterviewQuestionDifficulty Difficulty { get; private set; }
    public string? Notes { get; private set; }
    public InterviewQuestionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static InterviewQuestion Create(
        Guid id, Guid userId, string title, string question, string topic,
        InterviewQuestionDifficulty difficulty, string? notes,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Interview question id cannot be empty.",
                nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureValidDifficulty(difficulty);
        var normalizedTitle = InterviewQuestionText.NormalizeRequired(
            title, nameof(title), InterviewQuestionText.TitleMaxLength);
        var normalizedQuestion = InterviewQuestionText.NormalizeRequired(
            question, nameof(question), InterviewQuestionText.QuestionMaxLength);
        var normalizedTopic = InterviewQuestionText.NormalizeRequired(
            topic, nameof(topic), InterviewQuestionText.TopicMaxLength);
        var normalizedNotes = InterviewQuestionText.NormalizeOptional(
            notes, nameof(notes), InterviewQuestionText.NotesMaxLength);

        return new InterviewQuestion(
            id, userId, normalizedTitle, normalizedQuestion, normalizedTopic,
            difficulty, normalizedNotes, createdAtUtc);
    }

    public bool Update(
        string title, string question, string topic,
        InterviewQuestionDifficulty difficulty, string? notes,
        DateTimeOffset updatedAtUtc)
    {
        EnsureActive();
        EnsureValidDifficulty(difficulty);
        var normalizedTitle = InterviewQuestionText.NormalizeRequired(
            title, nameof(title), InterviewQuestionText.TitleMaxLength);
        var normalizedQuestion = InterviewQuestionText.NormalizeRequired(
            question, nameof(question), InterviewQuestionText.QuestionMaxLength);
        var normalizedTopic = InterviewQuestionText.NormalizeRequired(
            topic, nameof(topic), InterviewQuestionText.TopicMaxLength);
        var normalizedNotes = InterviewQuestionText.NormalizeOptional(
            notes, nameof(notes), InterviewQuestionText.NotesMaxLength);

        if (Title == normalizedTitle && Question == normalizedQuestion
            && Topic == normalizedTopic && Difficulty == difficulty
            && Notes == normalizedNotes)
        {
            return false;
        }

        Title = normalizedTitle;
        Question = normalizedQuestion;
        Topic = normalizedTopic;
        Difficulty = difficulty;
        Notes = normalizedNotes;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status == InterviewQuestionStatus.Archived)
        {
            return false;
        }

        Status = InterviewQuestionStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    private void EnsureActive()
    {
        if (Status == InterviewQuestionStatus.Archived)
        {
            throw new InvalidOperationException(
                InterviewQuestionErrors.Archived.Message);
        }
    }

    private static void EnsureValidDifficulty(
        InterviewQuestionDifficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(
                nameof(difficulty),
                InterviewQuestionErrors.InvalidDifficulty.Message);
        }
    }
}
