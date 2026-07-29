namespace DevRecall.Contracts.Analytics;

public sealed record ReviewPerformanceResponse(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalReviews, int AgainCount, int HardCount, int GoodCount,
    int EasyCount, decimal SuccessRate,
    decimal AveragePreviousIntervalDays,
    decimal AverageNextIntervalDays);
