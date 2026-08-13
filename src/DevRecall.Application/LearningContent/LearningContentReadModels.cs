namespace DevRecall.Application.LearningContent;

public sealed record LearningContentTechnologyItem(string Value, string Label);
public sealed record LearningContentTopicItem(string Slug, string Name);
public sealed record LearningContentObjectiveItem(int Position, string Text);
public sealed record LearningContentSectionItem(int Position, string Type, string? Heading, string BodyMarkdown);
public sealed record LearningContentSourceItem(string Type, string Name, string? Url);
public sealed record LearningContentReviewCandidateItem(
    string Key, string Prompt, string Answer, bool IsInReview);

public sealed record PublishedLearningContentListItem(
    string Slug, string Title, string Summary, string ContentType, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics, string ProgressStatus);

public sealed record PublishedLearningContentDetail(
    Guid Id, string Slug, string Title, string Summary, string ContentType, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics,
    IReadOnlyList<LearningContentObjectiveItem> Objectives,
    IReadOnlyList<LearningContentSectionItem> Sections,
    IReadOnlyList<LearningContentReviewCandidateItem> ReviewCandidates,
    LearningContentSourceItem Source, DateTimeOffset PublishedAtUtc,
    LearningContentProgressItem Progress);

public sealed record PublishedLearningContentPage(
    IReadOnlyList<PublishedLearningContentListItem> Items, int TotalCount);
public sealed record ContinueLearningContentItem(
    string Slug, string Title, string Summary, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyItem> Technologies, DateTimeOffset StartedAtUtc);
public sealed record LearningContentHistoryItem(
    Guid EvidenceId, string Title, DateTimeOffset CompletedAtUtc,
    string? SourceSlug, bool IsSourceAvailable);
public sealed record LearningContentHistoryPage(
    IReadOnlyList<LearningContentHistoryItem> Items, int TotalCount);

public interface ILearningContentReader
{
    Task<PublishedLearningContentPage> GetPublishedAsync(Guid userId, string? technology,
        string? topicSlug, string? difficulty, int skip, int take,
        CancellationToken cancellationToken);
    Task<PublishedLearningContentDetail?> GetPublishedBySlugAsync(Guid userId, string slug,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ContinueLearningContentItem>> GetInProgressAsync(Guid userId, int take,
        CancellationToken cancellationToken);
    Task<LearningContentHistoryPage> GetHistoryAsync(Guid userId, int skip, int take,
        CancellationToken cancellationToken);
}
