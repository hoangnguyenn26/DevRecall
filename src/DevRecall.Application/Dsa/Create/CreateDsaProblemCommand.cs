namespace DevRecall.Application.Dsa.Create;

public sealed record CreateDsaProblemCommand(
    string Title,
    string Description,
    string Difficulty,
    string? Source,
    string? ExternalUrl,
    IReadOnlyCollection<string>? Topics);
