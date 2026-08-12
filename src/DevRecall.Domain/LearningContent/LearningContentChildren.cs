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

public sealed record LearningObjectiveInput(string Text);
public sealed record LearningContentSectionInput(
    LearningContentSectionType SectionType, string? Heading, string BodyMarkdown);
