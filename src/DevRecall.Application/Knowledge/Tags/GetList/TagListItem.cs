namespace DevRecall.Application.Knowledge.Tags.GetList;

public sealed record TagListItem(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
