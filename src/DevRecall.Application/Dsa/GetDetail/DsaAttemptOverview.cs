namespace DevRecall.Application.Dsa.GetDetail;

public sealed record DsaAttemptOverview(
    Guid Id,
    int AttemptNumber,
    string Result,
    string? Language,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    DateTimeOffset AttemptedAtUtc);
