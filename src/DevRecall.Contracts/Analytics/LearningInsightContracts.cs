namespace DevRecall.Contracts.Analytics;

public sealed record LearningInsightSignalResponse(string Type, string Label, string Value);
public sealed record LearningInsightActionResponse(string Type, string Label, string TargetType, Guid? TargetId, bool IsAvailable);
public sealed record LearningInsightResponse(string Type, string Tone, string Priority, string Title, string Summary,
    IReadOnlyList<LearningInsightSignalResponse> Signals, LearningInsightActionResponse? Action);
public sealed record LearningInsightsResponse(DateTimeOffset GeneratedAtUtc, string Range, IReadOnlyList<LearningInsightResponse> Items);
