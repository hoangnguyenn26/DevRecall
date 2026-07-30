using DevRecall.Api.Authorization;
using DevRecall.Application.StudyPlans.Generate;
using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.StudyPlans;

namespace DevRecall.Api.Endpoints.StudyPlans;

public static class StudyPlanEndpoints
{
    public static IEndpointRouteBuilder MapStudyPlanEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/study-plans")
            .WithTags("Study Plans")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapPost("/generate", GenerateAsync)
            .WithName("GenerateStudyPlan")
            .Produces<GenerateStudyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        group.MapGet("/", GetListAsync)
            .WithName("GetStudyPlans")
            .Produces<PagedResponse<StudyPlanListItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/{studyPlanId:guid}", GetDetailAsync)
            .WithName("GetStudyPlanDetail")
            .Produces<StudyPlanDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> GetListAsync(
        [AsParameters] GetStudyPlansRequest request,
        GetStudyPlansHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new(request.Status, request.Page, request.PageSize),
            cancellationToken);
        return Results.Ok(new PagedResponse<StudyPlanListItemResponse>(
            result.Items.Select(item => new StudyPlanListItemResponse(
                item.StudyPlanId, item.Title, item.Status, item.ItemCount,
                item.TotalPlannedDurationMinutes, item.GeneratedAtUtc,
                item.ExpiresAtUtc, item.ReadyAtUtc, item.ConvertedAtUtc,
                item.ConvertedStudySessionId, item.CancelledAtUtc,
                item.UpdatedAtUtc, item.Version)).ToArray(),
            result.Page, result.PageSize, result.TotalCount,
            result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid studyPlanId,
        GetStudyPlanDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new(studyPlanId), cancellationToken);
        return Results.Ok(new StudyPlanDetailResponse(
            result.StudyPlanId, result.Title, result.Status, result.ItemCount,
            result.TotalPlannedDurationMinutes, result.GeneratedAtUtc,
            result.ExpiresAtUtc, result.ReadyAtUtc, result.ConvertedAtUtc,
            result.ConvertedStudySessionId, result.CancelledAtUtc,
            result.CreatedAtUtc, result.UpdatedAtUtc, result.Version,
            result.Items.Select(MapItem).ToArray()));
    }

    private static async Task<IResult> GenerateAsync(
        GenerateStudyPlanRequest request,
        GenerateStudyPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new(
                request.Title, request.TotalDurationMinutes,
                request.MaximumCandidates),
            cancellationToken);
        return Results.Ok(new GenerateStudyPlanResponse(
            result.StudyPlanId, result.Title, result.Status,
            result.TotalPlannedDurationMinutes, result.ItemCount,
            result.GeneratedAtUtc, result.ExpiresAtUtc, result.Version,
            result.Items.Select(item => new StudyPlanItemResponse(
                item.ItemId, item.SourceRecommendationId, item.SourceType,
                item.ResourceType, item.ResourceId, item.ResourceTitle,
                item.ResourcePreview, item.IsResourceAvailable,
                item.PlannedDurationMinutes, item.Position)).ToArray()));
    }

    private static StudyPlanItemResponse MapItem(StudyPlanDetailItem item) =>
        new(
            item.ItemId, item.SourceRecommendationId, item.SourceType,
            item.ResourceType, item.ResourceId, item.ResourceTitle,
            item.ResourcePreview, item.IsResourceAvailable,
            item.PlannedDurationMinutes, item.Position);
}
