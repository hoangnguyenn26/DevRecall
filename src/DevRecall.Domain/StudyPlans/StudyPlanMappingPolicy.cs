using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Study;

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

    public static StudyResourceType MapToStudyResourceType(
        StudyPlanResourceType resourceType) =>
        resourceType switch
        {
            StudyPlanResourceType.KnowledgeNode =>
                StudyResourceType.KnowledgeNode,
            StudyPlanResourceType.InterviewQuestion =>
                StudyResourceType.InterviewQuestion,
            StudyPlanResourceType.DsaProblem =>
                StudyResourceType.DsaProblem,
            StudyPlanResourceType.LearningContent =>
                StudyResourceType.LearningContent,
            _ => throw new ArgumentOutOfRangeException(
                nameof(resourceType), resourceType,
                "Unsupported study plan resource type.")
        };
}
