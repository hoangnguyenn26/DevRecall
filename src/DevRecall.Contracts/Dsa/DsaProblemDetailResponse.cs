namespace DevRecall.Contracts.Dsa;

public sealed record DsaProblemDetailResponse(
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
    DsaAttemptSummaryResponse AttemptSummary,
    DsaAttemptOverviewResponse? LatestAttempt,
    DsaAttemptOverviewResponse? LatestSuccessfulAttempt,
    IReadOnlyList<DsaAttemptOverviewResponse> RecentAttempts);
