namespace DevRecall.Contracts.Analytics;

public sealed record DailyActivityResponse(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    IReadOnlyList<DailyActivityDayResponse> Days);
public sealed record DailyActivityDayResponse(
    DateOnly Date, int StudyMinutes, int CompletedSessions,
    int CompletedStudyItems, int Reviews, int DsaAttempts);
