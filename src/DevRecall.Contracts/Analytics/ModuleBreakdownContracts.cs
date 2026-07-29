namespace DevRecall.Contracts.Analytics;

public sealed record ModuleBreakdownResponse(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalCompletedItems,
    IReadOnlyList<ModuleBreakdownItemResponse> Modules);
public sealed record ModuleBreakdownItemResponse(
    string ResourceType, int CompletedItems, decimal Percentage);
