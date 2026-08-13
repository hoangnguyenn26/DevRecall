namespace DevRecall.Contracts.Reviews;

public sealed record CreateLearningContentReviewsRequest(
    IReadOnlyList<string> CandidateKeys, Guid SubmissionId);
public sealed record LearningContentReviewBatchItemResponse(
    string CandidateKey, Guid ReviewItemId, bool WasCreated);
public sealed record LearningContentReviewBatchResponse(int CreatedCount, int ExistingCount,
    IReadOnlyList<LearningContentReviewBatchItemResponse> Items);
