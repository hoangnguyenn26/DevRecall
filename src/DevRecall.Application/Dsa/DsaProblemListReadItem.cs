using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa;

public sealed record DsaProblemListReadItem(
    Guid Id,
    string Title,
    DsaProblemDifficulty Difficulty,
    string? Source,
    DateTimeOffset UpdatedAtUtc);
