namespace DevRecall.Contracts.LearningContent;

public sealed class GetLearningContentRequest
{
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public string? Difficulty { get; init; }
    public string? ContentType { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed record LearningContentTechnologyResponse(string Value, string Label);
public sealed record LearningContentTopicResponse(string Slug, string Name);
public sealed record LearningContentObjectiveResponse(int Position, string Text);
public sealed record LearningContentSectionResponse(int Position, string Type, string? Heading, string BodyMarkdown);
public sealed record LearningContentSourceResponse(string Type, string Name, string? Url);
public sealed record LearningContentReviewCandidateResponse(
    string Key, string Prompt, string Answer, bool IsInReview);
public sealed record LearningContentListItemResponse(string Slug, string Title, string Summary,
    string ContentType, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics, string? ProgressStatus,
    string? ResourceKind = null, string? SourceName = null);
public sealed record LearningContentProgressResponse(string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, int? Version, Guid? CompletionEvidenceId);
public sealed record CompleteLearningContentRequest(int? ExpectedVersion);
public sealed record LearningContentDetailResponse(Guid Id, string Slug, string Title, string Summary,
    string ContentType, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics,
    IReadOnlyList<LearningContentObjectiveResponse> Objectives,
    IReadOnlyList<LearningContentSectionResponse> Sections,
    IReadOnlyList<LearningContentReviewCandidateResponse> ReviewCandidates,
    LearningContentSourceResponse Source, DateTimeOffset PublishedAtUtc,
    LearningContentProgressResponse? Progress, string? ResourceKind = null, IReadOnlyList<string>? Goals = null);
public sealed record ContinueLearningContentResponse(
    string Slug, string Title, string Summary, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyResponse> Technologies, DateTimeOffset StartedAtUtc);
public sealed record LearningContentHistoryItemResponse(
    Guid EvidenceId, string Title, DateTimeOffset CompletedAtUtc,
    string? SourceSlug, bool IsSourceAvailable);
