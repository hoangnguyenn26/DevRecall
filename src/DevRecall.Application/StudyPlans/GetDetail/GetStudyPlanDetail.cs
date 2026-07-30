using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.GetDetail;

public sealed record GetStudyPlanDetailQuery(Guid StudyPlanId);

public sealed record StudyPlanItemReadModel(
    Guid ItemId, Guid? SourceRecommendationId,
    StudyPlanSourceType SourceType, StudyPlanResourceType ResourceType,
    Guid ResourceId, int PlannedDurationMinutes, int Position);

public sealed record StudyPlanDetailReadModel(
    Guid StudyPlanId, string Title, StudyPlanStatus Status,
    DateTimeOffset GeneratedAtUtc, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId, DateTimeOffset? CancelledAtUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    int Version, IReadOnlyList<StudyPlanItemReadModel> Items);

public sealed record StudyPlanDetailItem(
    Guid ItemId, Guid? SourceRecommendationId, string SourceType,
    string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable,
    int PlannedDurationMinutes, int Position);

public sealed record GetStudyPlanDetailResult(
    Guid StudyPlanId, string Title, string Status,
    int ItemCount, int TotalPlannedDurationMinutes,
    DateTimeOffset GeneratedAtUtc, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId, DateTimeOffset? CancelledAtUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    int Version, IReadOnlyList<StudyPlanDetailItem> Items);

public interface IStudyPlanDetailReader
{
    Task<StudyPlanDetailReadModel?> FindAsync(
        Guid userId, Guid studyPlanId, CancellationToken cancellationToken);
}

public sealed class GetStudyPlanDetailHandler(
    IStudyPlanDetailReader detailReader,
    IStudyPlanResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    public async Task<GetStudyPlanDetailResult> HandleAsync(
        GetStudyPlanDetailQuery query, CancellationToken cancellationToken)
    {
        if (query.StudyPlanId == Guid.Empty)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["studyPlanId"] = ["Study plan id is required."]
            });
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "AUTH_REQUIRED", "Authentication is required.");
        var plan = await detailReader.FindAsync(
            userId, query.StudyPlanId, cancellationToken);
        if (plan is null)
        {
            throw new NotFoundException(
                StudyPlanErrors.NotFound.Code, StudyPlanErrors.NotFound.Message);
        }

        var references = plan.Items.Select(item =>
            new StudyPlanResourceReference(
                item.ResourceType, item.ResourceId)).Distinct().ToArray();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var byKey = summaries.GroupBy(
                item => (item.ResourceType, item.ResourceId))
            .ToDictionary(group => group.Key, group => group.First());
        var items = plan.Items
            .OrderBy(item => item.Position).ThenBy(item => item.ItemId)
            .Select(item =>
            {
                byKey.TryGetValue(
                    (item.ResourceType, item.ResourceId), out var resource);
                return new StudyPlanDetailItem(
                    item.ItemId, item.SourceRecommendationId,
                    item.SourceType.ToString(), item.ResourceType.ToString(),
                    item.ResourceId,
                    resource?.Title ?? "Unavailable resource",
                    resource?.Preview, resource?.IsAvailable ?? false,
                    item.PlannedDurationMinutes, item.Position);
            }).ToArray();
        return new(
            plan.StudyPlanId, plan.Title, plan.Status.ToString(), items.Length,
            items.Sum(item => item.PlannedDurationMinutes),
            plan.GeneratedAtUtc, plan.ExpiresAtUtc, plan.ReadyAtUtc,
            plan.ConvertedAtUtc, plan.ConvertedStudySessionId,
            plan.CancelledAtUtc, plan.CreatedAtUtc, plan.UpdatedAtUtc,
            plan.Version, items);
    }
}
