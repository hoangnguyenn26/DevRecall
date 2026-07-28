namespace DevRecall.Contracts.Dsa.Attempts;

public sealed record CompareDsaAttemptsResponse(
    Guid DsaProblemId,
    DsaAttemptComparisonSnapshotResponse Left,
    DsaAttemptComparisonSnapshotResponse Right,
    DsaAttemptComparisonDifferenceResponse Difference);
