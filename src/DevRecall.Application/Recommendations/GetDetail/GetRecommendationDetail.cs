using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Recommendations.GetDetail;

public sealed record GetRecommendationDetailQuery(Guid RecommendationId);
public sealed record RecommendationDetailReadModel(
    Guid RecommendationId, RecommendationResourceType ResourceType,
    Guid ResourceId, RecommendationType Type, RecommendationPriority Priority,
    decimal PriorityScore, RecommendationStatus Status, decimal WeaknessScore,
    WeaknessLevel WeaknessLevel, int SignalCount,
    DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? DismissedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? ExpiredAtUtc,
    RecommendationExpirationReason? ExpirationReason,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);
public sealed record GetRecommendationDetailResult(
    Guid RecommendationId, string ResourceType, Guid ResourceId,
    string ResourceTitle, string? ResourcePreview, bool IsResourceAvailable,
    string Type, string Priority, decimal PriorityScore, string Status,
    decimal WeaknessScore, string WeaknessLevel, int SignalCount,
    DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? DismissedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? ExpiredAtUtc,
    string? ExpirationReason,
    IReadOnlyList<RecommendationReasonItem> Reasons,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc, int Version);
public sealed record RecommendationReasonItem(
    string Type, int? Count = null, string? Level = null,
    DateTimeOffset? CalculatedAtUtc = null);

public interface IRecommendationDetailReader
{
    Task<RecommendationDetailReadModel?> FindAsync(
        Guid userId, Guid recommendationId, CancellationToken cancellationToken);
}

public sealed class GetRecommendationDetailHandler(
    IRecommendationDetailReader detailReader,
    IRecommendationResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    public async Task<GetRecommendationDetailResult> HandleAsync(
        GetRecommendationDetailQuery query, CancellationToken cancellationToken)
    {
        if (query.RecommendationId == Guid.Empty)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["recommendationId"] = ["Recommendation id is required."]
            });
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var item = await detailReader.FindAsync(
            userId, query.RecommendationId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(
                RecommendationErrors.NotFound.Code,
                RecommendationErrors.NotFound.Message);
        }

        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, [new(item.ResourceType, item.ResourceId)], cancellationToken);
        var resource = summaries.SingleOrDefault();
        RecommendationReasonItem[] reasons =
        [
            new("WeakTopicSeverity", Level: item.WeaknessLevel.ToString(),
                CalculatedAtUtc: item.WeaknessCalculatedAtUtc),
            new("ContributingSignals", Count: item.SignalCount)
        ];
        return new(
            item.RecommendationId, item.ResourceType.ToString(), item.ResourceId,
            resource?.Title ?? "Unavailable resource", resource?.Preview,
            resource?.IsAvailable ?? false, item.Type.ToString(),
            item.Priority.ToString(), item.PriorityScore, item.Status.ToString(),
            item.WeaknessScore, item.WeaknessLevel.ToString(), item.SignalCount,
            item.WeaknessCalculatedAtUtc, item.GeneratedAtUtc, item.ExpiresAtUtc,
            item.DismissedAtUtc, item.CompletedAtUtc, item.ExpiredAtUtc,
            item.ExpirationReason?.ToString(), reasons, item.CreatedAtUtc,
            item.UpdatedAtUtc, item.Version);
    }
}
