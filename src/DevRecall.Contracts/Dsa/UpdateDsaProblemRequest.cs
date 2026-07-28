namespace DevRecall.Contracts.Dsa;

public sealed record UpdateDsaProblemRequest(
    string Title,
    string Description,
    string Difficulty,
    string? Source,
    string? ExternalUrl,
    IReadOnlyCollection<string>? Topics);
