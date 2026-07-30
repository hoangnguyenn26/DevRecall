using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics.GetList;

public sealed record GetWeakTopicsQuery(
    string? Level, string? ResourceType, decimal? MinimumScore,
    bool IncludeNone, int Page, int PageSize);
public sealed record WeakTopicListReadModel(
    Guid ProfileId, WeakTopicResourceType ResourceType, Guid ResourceId,
    decimal Score, WeaknessLevel Level, int SignalCount, int Version,
    DateTimeOffset CalculatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record WeakTopicListItem(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable, decimal Score, string Level,
    int SignalCount, int Version, DateTimeOffset CalculatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record GetWeakTopicsResult(
    IReadOnlyList<WeakTopicListItem> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);
public sealed record WeakTopicResourceReference(
    WeakTopicResourceType ResourceType, Guid ResourceId);
public sealed record WeakTopicResourceSummary(
    WeakTopicResourceType ResourceType, Guid ResourceId, string Title,
    string? Preview, bool IsAvailable);

public interface IWeakTopicListReader
{
    Task<PagedReadResult<WeakTopicListReadModel>> ReadAsync(
        Guid userId, WeaknessLevel? level, WeakTopicResourceType? resourceType,
        decimal? minimumScore, bool includeNone, int skip, int take,
        CancellationToken cancellationToken);
}

public interface IWeakTopicResourceSummaryReader
{
    Task<IReadOnlyList<WeakTopicResourceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<WeakTopicResourceReference> resources,
        CancellationToken cancellationToken);
}

public sealed class GetWeakTopicsHandler(
    IWeakTopicListReader listReader,
    IWeakTopicResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    public async Task<GetWeakTopicsResult> HandleAsync(
        GetWeakTopicsQuery query, CancellationToken cancellationToken)
    {
        Validate(query);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        WeaknessLevel? level = string.IsNullOrWhiteSpace(query.Level)
            ? null : WeaknessLevelParser.Parse(query.Level);
        WeakTopicResourceType? resourceType = string.IsNullOrWhiteSpace(query.ResourceType)
            ? null : WeakTopicResourceTypeParser.Parse(query.ResourceType);
        var skip = checked((query.Page - 1) * query.PageSize);
        var page = await listReader.ReadAsync(
            userId, level, resourceType, query.MinimumScore, query.IncludeNone,
            skip, query.PageSize, cancellationToken);
        var references = page.Items.Select(x =>
            new WeakTopicResourceReference(x.ResourceType, x.ResourceId)).ToArray();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var byResource = summaries.ToDictionary(x => (x.ResourceType, x.ResourceId));
        var items = page.Items.Select(profile =>
        {
            byResource.TryGetValue(
                (profile.ResourceType, profile.ResourceId), out var resource);
            return new WeakTopicListItem(
                profile.ProfileId, profile.ResourceType.ToString(), profile.ResourceId,
                resource?.Title ?? "Unavailable resource", resource?.Preview,
                resource?.IsAvailable ?? false, profile.Score, profile.Level.ToString(),
                profile.SignalCount, profile.Version, profile.CalculatedAtUtc,
                profile.UpdatedAtUtc);
        }).ToArray();
        var totalPages = page.TotalCount == 0
            ? 0 : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize);
        return new(items, query.Page, query.PageSize, page.TotalCount, totalPages);
    }

    private static void Validate(GetWeakTopicsQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        if (query.MinimumScore < 0)
        {
            errors["minimumScore"] = ["Minimum score must be at least 0."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
