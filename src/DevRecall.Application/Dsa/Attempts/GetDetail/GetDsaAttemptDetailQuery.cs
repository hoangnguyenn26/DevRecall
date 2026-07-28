namespace DevRecall.Application.Dsa.Attempts.GetDetail;

public sealed record GetDsaAttemptDetailQuery(
    Guid DsaProblemId,
    Guid DsaAttemptId);
