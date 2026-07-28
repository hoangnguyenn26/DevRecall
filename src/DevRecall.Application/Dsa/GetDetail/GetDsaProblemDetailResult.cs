namespace DevRecall.Application.Dsa.GetDetail;

public sealed record GetDsaProblemDetailResult(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    string? Source,
    string? ExternalUrl,
    IReadOnlyList<string> Topics,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DsaAttemptSummary AttemptSummary,
    DsaAttemptOverview? LatestAttempt,
    DsaAttemptOverview? LatestSuccessfulAttempt,
    IReadOnlyList<DsaAttemptOverview> RecentAttempts);
