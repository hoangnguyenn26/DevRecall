namespace DevRecall.Application.Dsa.Attempts.GetList;

public sealed record DsaAttemptListItem(
    Guid Id,
    int AttemptNumber,
    string Result,
    string? Language,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    DateTimeOffset AttemptedAtUtc,
    DateTimeOffset CreatedAtUtc);
