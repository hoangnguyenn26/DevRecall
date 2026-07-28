namespace DevRecall.Contracts.Reviews;

public sealed record EvaluateReviewItemRequest(
    string Evaluation, int ExpectedReviewCount);
