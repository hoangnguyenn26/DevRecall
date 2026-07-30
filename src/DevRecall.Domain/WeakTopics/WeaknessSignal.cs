namespace DevRecall.Domain.WeakTopics;

public sealed record WeaknessSignal(
    WeaknessSignalType Type, DateTimeOffset OccurredAtUtc);

public sealed record WeaknessSignalContribution(
    WeaknessSignalType SignalType, int BaseWeight,
    decimal RecencyMultiplier, decimal WeightedScore);
