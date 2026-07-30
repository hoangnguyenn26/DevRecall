namespace DevRecall.Domain.WeakTopics;

public sealed record WeaknessScoreBreakdown(
    decimal RawScore, decimal FinalScore, WeaknessLevel Level,
    int SignalCount, DateTimeOffset CalculatedAtUtc,
    IReadOnlyList<WeaknessSignalContribution> Contributions);
