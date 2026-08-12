namespace DevRecall.Contracts.LearningContent;

public sealed class GetLearningContentRequest
{
    public string? Technology { get; init; }
    public string? Topic { get; init; }
    public string? Difficulty { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed record LearningContentTechnologyResponse(string Value, string Label);
public sealed record LearningContentTopicResponse(string Slug, string Name);
public sealed record LearningContentObjectiveResponse(int Position, string Text);
public sealed record LearningContentSectionResponse(int Position, string Type, string? Heading, string BodyMarkdown);
public sealed record LearningContentSourceResponse(string Type, string Name, string? Url);
public sealed record LearningContentListItemResponse(string Slug, string Title, string Summary,
    string ContentType, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics);
public sealed record LearningContentDetailResponse(string Slug, string Title, string Summary,
    string ContentType, string Difficulty, int EstimatedMinutes,
    IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics,
    IReadOnlyList<LearningContentObjectiveResponse> Objectives,
    IReadOnlyList<LearningContentSectionResponse> Sections,
    LearningContentSourceResponse Source, DateTimeOffset PublishedAtUtc);
