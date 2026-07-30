using DevRecall.Domain.Recommendations;

namespace DevRecall.Domain.StudyPlans;

public static class StudyPlanMappingPolicy
{
    public static StudyPlanResourceType MapResourceType(
        RecommendationResourceType resourceType) =>
        resourceType switch
        {
            RecommendationResourceType.KnowledgeNode =>
                StudyPlanResourceType.KnowledgeNode,
            RecommendationResourceType.InterviewQuestion =>
                StudyPlanResourceType.InterviewQuestion,
            RecommendationResourceType.DsaProblem =>
                StudyPlanResourceType.DsaProblem,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resourceType), resourceType,
                "Unsupported recommendation resource type.")
        };
}
