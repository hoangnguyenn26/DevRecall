namespace DevRecall.Application.Knowledge.Tags.Create;

public sealed record CreateTagResult(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
