using DevRecall.Domain.WeakTopics;

namespace DevRecall.Domain.Recommendations;

public static class RecommendationPriorityPolicy
{
    public static RecommendationPriority FromWeaknessLevel(
        WeaknessLevel weaknessLevel) =>
        weaknessLevel switch
        {
            WeaknessLevel.Low => RecommendationPriority.Low,
            WeaknessLevel.Medium => RecommendationPriority.Medium,
            WeaknessLevel.High => RecommendationPriority.High,
            WeaknessLevel.Critical => RecommendationPriority.Critical,
            WeaknessLevel.None => throw new ArgumentException(
                "A recommendation cannot be created from a weakness level of None.",
                nameof(weaknessLevel)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(weaknessLevel), weaknessLevel,
                "Unsupported weakness level.")
        };
}

public static class RecommendationMappingPolicy
{
    public static RecommendationResourceType MapResourceType(
        WeakTopicResourceType resourceType) =>
        resourceType switch
        {
            WeakTopicResourceType.KnowledgeNode =>
                RecommendationResourceType.KnowledgeNode,
            WeakTopicResourceType.InterviewQuestion =>
                RecommendationResourceType.InterviewQuestion,
            WeakTopicResourceType.DsaProblem =>
                RecommendationResourceType.DsaProblem,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
        };

    public static RecommendationType MapType(WeakTopicResourceType resourceType) =>
        resourceType switch
        {
            WeakTopicResourceType.KnowledgeNode =>
                RecommendationType.ReviewKnowledge,
            WeakTopicResourceType.InterviewQuestion =>
                RecommendationType.PracticeInterview,
            WeakTopicResourceType.DsaProblem =>
                RecommendationType.RetryDsaProblem,
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
        };
}

public static class RecommendationDefaults
{
    public static readonly TimeSpan ActiveLifetime = TimeSpan.FromDays(14);
}
