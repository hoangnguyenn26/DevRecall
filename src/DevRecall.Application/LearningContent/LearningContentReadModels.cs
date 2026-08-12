namespace DevRecall.Application.LearningContent;

public sealed record LearningContentTechnologyItem(string Value, string Label);
public sealed record LearningContentTopicItem(string Slug, string Name);
public sealed record LearningContentObjectiveItem(int Position, string Text);
public sealed record LearningContentSectionItem(int Position, string Type, string? Heading, string BodyMarkdown);
public sealed record LearningContentSourceItem(string Type, string Name, string? Url);

public sealed record PublishedLearningContentListItem(
    string Slug, string Title, string Summary, string ContentType, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics);

public sealed record PublishedLearningContentDetail(
    string Slug, string Title, string Summary, string ContentType, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics,
    IReadOnlyList<LearningContentObjectiveItem> Objectives,
    IReadOnlyList<LearningContentSectionItem> Sections,
    LearningContentSourceItem Source, DateTimeOffset PublishedAtUtc);

public sealed record PublishedLearningContentPage(
    IReadOnlyList<PublishedLearningContentListItem> Items, int TotalCount);

public interface ILearningContentReader
{
    Task<PublishedLearningContentPage> GetPublishedAsync(string? technology,
        string? topicSlug, string? difficulty, int skip, int take,
        CancellationToken cancellationToken);
    Task<PublishedLearningContentDetail?> GetPublishedBySlugAsync(string slug,
        CancellationToken cancellationToken);
}
