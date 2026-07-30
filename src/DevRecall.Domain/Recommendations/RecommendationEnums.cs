namespace DevRecall.Domain.Recommendations;

public enum RecommendationResourceType
{
    KnowledgeNode = 1,
    InterviewQuestion = 2,
    DsaProblem = 3
}

public enum RecommendationType
{
    ReviewKnowledge = 1,
    PracticeInterview = 2,
    RetryDsaProblem = 3
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum RecommendationStatus
{
    Active = 1,
    Dismissed = 2,
    Completed = 3,
    Expired = 4
}

public enum RecommendationExpirationReason
{
    LifetimeElapsed = 1,
    WeaknessResolved = 2,
    ResourceUnavailable = 3
}
