namespace DevRecall.Application.Dsa.Attempts.Compare;

public sealed record CompareDsaAttemptsQuery(
    Guid DsaProblemId,
    Guid LeftAttemptId,
    Guid RightAttemptId);
