namespace DevRecall.Contracts.Dsa.Practice;

public sealed record GetDsaPracticeResponse(
    Guid ProblemId, string Title, string? Description, string? ExternalUrl,
    string Difficulty, IReadOnlyList<string> Topics, int ProblemVersion);
