using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Domain.LearningContent;

public sealed class LearningContentTechnology
{
    private LearningContentTechnology() { }
    internal LearningContentTechnology(Guid id, Guid learningContentId, Technology technology)
    {
        Id = id;
        LearningContentId = learningContentId;
        Technology = technology;
    }
    public Guid Id { get; private set; }
    public Guid LearningContentId { get; private set; }
    public Technology Technology { get; private set; }
}

public sealed class LearningContentTopic
{
    private LearningContentTopic() { }
    internal LearningContentTopic(Guid learningContentId, Guid topicId)
    {
        LearningContentId = learningContentId;
        TopicId = topicId;
    }
    public Guid LearningContentId { get; private set; }
    public Guid TopicId { get; private set; }
}

public sealed class LearningObjective
{
    private LearningObjective() { }
    internal LearningObjective(Guid id, Guid learningContentId, int position, string text)
    {
        Id = id;
        LearningContentId = learningContentId;
        Position = position;
        Text = text;
    }
    public Guid Id { get; private set; }
    public Guid LearningContentId { get; private set; }
    public int Position { get; private set; }
    public string Text { get; private set; } = null!;
}

public sealed class LearningContentSection
{
    private LearningContentSection() { }
    internal LearningContentSection(Guid id, Guid learningContentId, int position,
        LearningContentSectionType sectionType, string? heading, string bodyMarkdown)
    {
        Id = id;
        LearningContentId = learningContentId;
        Position = position;
        SectionType = sectionType;
        Heading = heading;
        BodyMarkdown = bodyMarkdown;
    }
    public Guid Id { get; private set; }
    public Guid LearningContentId { get; private set; }
    public int Position { get; private set; }
    public LearningContentSectionType SectionType { get; private set; }
    public string? Heading { get; private set; }
    public string BodyMarkdown { get; private set; } = null!;
}

public sealed class LearningReviewCandidate
{
    private LearningReviewCandidate() { }
    internal LearningReviewCandidate(Guid id, Guid learningContentId, int position,
        string key, string prompt, string answer)
    {
        Id = id;
        LearningContentId = learningContentId;
        Position = position;
        Key = key;
        Prompt = prompt;
        Answer = answer;
    }
    public Guid Id { get; private set; }
    public Guid LearningContentId { get; private set; }
    public int Position { get; private set; }
    public string Key { get; private set; } = null!;
    public string Prompt { get; private set; } = null!;
    public string Answer { get; private set; } = null!;

    public static LearningReviewCandidate Create(Guid id, Guid learningContentId, int position,
        string key, string prompt, string answer)
    {
        if (id == Guid.Empty || learningContentId == Guid.Empty)
            throw new ArgumentException("Review candidate identifiers cannot be empty.");
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        return new(id, learningContentId, position, NormalizeKey(key),
            LearningContentText.NormalizeRequired(prompt, nameof(prompt), 500),
            LearningContentText.NormalizeRequired(answer, nameof(answer), 2_000));
    }

    internal static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Review candidate key is required.", nameof(value));
        var key = value.Trim();
        if (key.Length > 100 || key.Any(character =>
            !((character is >= 'a' and <= 'z') || char.IsAsciiDigit(character) || character == '-'))
            || key[0] == '-' || key[^1] == '-' || key.Contains("--", StringComparison.Ordinal))
            throw new ArgumentException("Review candidate key must be a lowercase hyphenated value.", nameof(value));
        return key;
    }
}

public sealed record LearningObjectiveInput(string Text);
public sealed record LearningContentSectionInput(
    LearningContentSectionType SectionType, string? Heading, string BodyMarkdown);
public sealed record LearningReviewCandidateInput(string Key, string Prompt, string Answer);
