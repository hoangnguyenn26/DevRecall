namespace DevRecall.Application.Dsa.Update;

public sealed record UpdateDsaProblemCommand(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    string? Source,
    string? ExternalUrl,
    IReadOnlyCollection<string>? Topics);
