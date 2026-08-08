namespace DevRecall.Application.Reviews.Evaluate;

public sealed record EvaluateReviewItemCommand(
    Guid ReviewItemId, string Evaluation, int ExpectedReviewCount,
    Guid SubmissionId);
