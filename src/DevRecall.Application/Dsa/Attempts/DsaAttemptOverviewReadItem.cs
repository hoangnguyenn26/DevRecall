using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts;

public sealed record DsaAttemptOverviewReadItem(
    Guid Id,
    int AttemptNumber,
    DsaAttemptResult Result,
    string? Language,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    DateTimeOffset AttemptedAtUtc);
