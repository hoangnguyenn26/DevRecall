using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Recommendations.GetList;

public sealed record GetRecommendationsQuery(
    string? Status, string? Priority, string? ResourceType, string? Type,
    decimal? MinimumPriorityScore, int Page, int PageSize);
public sealed record RecommendationListReadModel(
    Guid RecommendationId, RecommendationResourceType ResourceType,
    Guid ResourceId, RecommendationType Type, RecommendationPriority Priority,
    decimal PriorityScore, RecommendationStatus Status, decimal WeaknessScore,
    WeaknessLevel WeaknessLevel, int SignalCount,
    DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? DismissedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? ExpiredAtUtc,
    RecommendationExpirationReason? ExpirationReason, int Version);
public sealed record RecommendationListItem(
    Guid RecommendationId, string ResourceType, Guid ResourceId,
    string ResourceTitle, string? ResourcePreview, bool IsResourceAvailable,
    string Type, string Priority, decimal PriorityScore, string Status,
    decimal WeaknessScore, string WeaknessLevel, int SignalCount,
    DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? DismissedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? ExpiredAtUtc,
    string? ExpirationReason, int Version);
public sealed record GetRecommendationsResult(
    IReadOnlyList<RecommendationListItem> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);
public sealed record RecommendationResourceReference(
    RecommendationResourceType ResourceType, Guid ResourceId);
public sealed record RecommendationResourceSummary(
    RecommendationResourceType ResourceType, Guid ResourceId,
    string Title, string? Preview, bool IsAvailable);

public interface IRecommendationListReader
{
    Task<PagedReadResult<RecommendationListReadModel>> ReadAsync(
        Guid userId, RecommendationStatus status,
        RecommendationPriority? priority,
        RecommendationResourceType? resourceType, RecommendationType? type,
        decimal? minimumPriorityScore, int skip, int take,
        CancellationToken cancellationToken);
}

public interface IRecommendationResourceSummaryReader
{
    Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<RecommendationResourceReference> resources,
        CancellationToken cancellationToken);
}

public sealed class GetRecommendationsHandler(
    IRecommendationListReader listReader,
    IRecommendationResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    public async Task<GetRecommendationsResult> HandleAsync(
        GetRecommendationsQuery query, CancellationToken cancellationToken)
    {
        Validate(query);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var status = string.IsNullOrWhiteSpace(query.Status)
            ? RecommendationStatus.Active
            : RecommendationParsers.ParseStatus(query.Status);
        RecommendationPriority? priority = string.IsNullOrWhiteSpace(query.Priority)
            ? null : RecommendationParsers.ParsePriority(query.Priority);
        RecommendationResourceType? resourceType =
            string.IsNullOrWhiteSpace(query.ResourceType)
                ? null : RecommendationParsers.ParseResourceType(query.ResourceType);
        RecommendationType? type = string.IsNullOrWhiteSpace(query.Type)
            ? null : RecommendationParsers.ParseType(query.Type);
        var skip = checked((query.Page - 1) * query.PageSize);
        var page = await listReader.ReadAsync(
            userId, status, priority, resourceType, type,
            query.MinimumPriorityScore, skip, query.PageSize, cancellationToken);
        var references = page.Items.Select(x =>
            new RecommendationResourceReference(x.ResourceType, x.ResourceId))
            .Distinct().ToArray();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var summaryByKey = summaries.ToDictionary(
            x => (x.ResourceType, x.ResourceId));
        var items = page.Items.Select(x =>
        {
            summaryByKey.TryGetValue((x.ResourceType, x.ResourceId), out var resource);
            return new RecommendationListItem(
                x.RecommendationId, x.ResourceType.ToString(), x.ResourceId,
                resource?.Title ?? "Unavailable resource", resource?.Preview,
                resource?.IsAvailable ?? false, x.Type.ToString(),
                x.Priority.ToString(), x.PriorityScore, x.Status.ToString(),
                x.WeaknessScore, x.WeaknessLevel.ToString(), x.SignalCount,
                x.WeaknessCalculatedAtUtc, x.GeneratedAtUtc, x.ExpiresAtUtc,
                x.DismissedAtUtc, x.CompletedAtUtc, x.ExpiredAtUtc,
                x.ExpirationReason?.ToString(), x.Version);
        }).ToArray();
        var totalPages = page.TotalCount == 0
            ? 0 : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize);
        return new(items, query.Page, query.PageSize, page.TotalCount, totalPages);
    }

    private static void Validate(GetRecommendationsQuery query)
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

        if (query.MinimumPriorityScore < 0)
        {
            errors["minimumPriorityScore"] =
                ["Minimum priority score must be at least 0."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

public static class RecommendationParsers
{
    public static RecommendationStatus ParseStatus(string value) =>
        Normalize(value) switch
        {
            "active" => RecommendationStatus.Active,
            "dismissed" => RecommendationStatus.Dismissed,
            "completed" => RecommendationStatus.Completed,
            "expired" => RecommendationStatus.Expired,
            _ => throw Invalid("status", "Status is invalid.")
        };

    public static RecommendationPriority ParsePriority(string value) =>
        Normalize(value) switch
        {
            "low" => RecommendationPriority.Low,
            "medium" => RecommendationPriority.Medium,
            "high" => RecommendationPriority.High,
            "critical" => RecommendationPriority.Critical,
            _ => throw Invalid("priority", "Priority is invalid.")
        };

    public static RecommendationResourceType ParseResourceType(string value) =>
        Normalize(value) switch
        {
            "knowledgenode" => RecommendationResourceType.KnowledgeNode,
            "interviewquestion" => RecommendationResourceType.InterviewQuestion,
            "dsaproblem" => RecommendationResourceType.DsaProblem,
            _ => throw Invalid("resourceType", "Resource type is invalid.")
        };

    public static RecommendationType ParseType(string value) =>
        Normalize(value) switch
        {
            "reviewknowledge" => RecommendationType.ReviewKnowledge,
            "practiceinterview" => RecommendationType.PracticeInterview,
            "retrydsaproblem" => RecommendationType.RetryDsaProblem,
            _ => throw Invalid("type", "Recommendation type is invalid.")
        };

    private static string Normalize(string value) =>
        value.Trim().Replace(" ", string.Empty)
            .Replace("-", string.Empty).Replace("_", string.Empty)
            .ToLowerInvariant();

    private static ValidationException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
