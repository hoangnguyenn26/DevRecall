namespace DevRecall.Application.Dsa.Attempts.Compare;

public sealed record CompareDsaAttemptsResult(
    Guid DsaProblemId,
    DsaAttemptComparisonSnapshot Left,
    DsaAttemptComparisonSnapshot Right,
    DsaAttemptComparisonDifference Difference);
