using DevRecall.Contracts.LearningContent;
using DevRecall.Contracts.LearningProfiles;

namespace DevRecall.Contracts.Discover;

public sealed record DiscoverReasonResponse(string Type, LearningProfileValueResponse? Goal,
    string? Value, string? Label, int? AvailableMinutes);
public sealed record DiscoverLessonResponse(string Slug, string Title, string Summary, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics, IReadOnlyList<DiscoverReasonResponse> Reasons);
public sealed record DiscoverResponse(bool ProfileConfigured, IReadOnlyList<DiscoverLessonResponse> BasedOnGoals,
    IReadOnlyList<DiscoverLessonResponse> BasedOnWeakTopics, IReadOnlyList<DiscoverLessonResponse> Recommended)
{
    public IReadOnlyList<DiscoverResourceResponse> TrustedResources { get; init; } = [];
}
public sealed record DiscoverResourceResponse(string Slug, string Title, string Summary, string ResourceKind, string SourceName,
    string Difficulty, int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyResponse> Technologies,
    IReadOnlyList<LearningContentTopicResponse> Topics, IReadOnlyList<DiscoverReasonResponse> Reasons);
