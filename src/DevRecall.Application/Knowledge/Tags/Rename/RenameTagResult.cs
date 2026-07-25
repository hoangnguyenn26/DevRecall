namespace DevRecall.Application.Knowledge.Tags.Rename;

public sealed record RenameTagResult(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
