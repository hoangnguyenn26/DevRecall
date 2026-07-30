namespace DevRecall.Contracts.Recommendations;

public sealed class GenerateRecommendationsRequest
{
    public int MaximumCandidates { get; init; } = 100;
}

public sealed record GenerateRecommendationsResponse(
    DateTimeOffset GeneratedAtUtc, int CandidateCount,
    int CreatedCount, int UpdatedCount, int UnchangedCount,
    int LowPriorityCount, int MediumPriorityCount,
    int HighPriorityCount, int CriticalPriorityCount);
