using DevRecall.Api.Authorization;
using DevRecall.Application.StudyPlans.Convert;
using DevRecall.Application.StudyPlans.Generate;
using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Application.StudyPlans.LearningContent;
using DevRecall.Application.StudyPlans.Mutations;
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
        group.MapPut("/{studyPlanId:guid}", UpdateAsync)
            .WithName("UpdateStudyPlan")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{studyPlanId:guid}/draft", ReplaceDraftAsync)
            .WithName("ReplaceStudyPlanDraft")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        group.MapPut("/{studyPlanId:guid}/items/{itemId:guid}", UpdateItemAsync)
            .WithName("UpdateStudyPlanItem")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{studyPlanId:guid}/items/{itemId:guid}", RemoveItemAsync)
            .WithName("RemoveStudyPlanItem")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{studyPlanId:guid}/items/reorder", ReorderItemsAsync)
            .WithName("ReorderStudyPlanItems")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{studyPlanId:guid}/ready", MarkReadyAsync)
            .WithName("MarkStudyPlanReady")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{studyPlanId:guid}/cancel", CancelAsync)
            .WithName("CancelStudyPlan")
            .Produces<StudyPlanMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{studyPlanId:guid}/convert", ConvertAsync)
            .WithName("ConvertStudyPlan")
            .Produces<ConvertStudyPlanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{studyPlanId:guid}/learning-content/{slug}", AddLearningContentAsync)
            .WithName("AddLearningContentToStudyPlan")
            .Produces<AddLearningContentToStudyPlanResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/learning-content/{slug}/options", GetLearningContentOptionsAsync)
            .WithName("GetLearningContentStudyPlanOptions")
            .Produces<IReadOnlyList<LearningContentStudyPlanOptionResponse>>();
        return endpoints;
    }

    private static async Task<IResult> GetLearningContentOptionsAsync(string slug,
        GetLearningContentStudyPlanOptionsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(slug, cancellationToken);
        return Results.Ok(result.Select(item => new LearningContentStudyPlanOptionResponse(item.StudyPlanId,
            item.Title, item.ItemCount, item.TotalPlannedDurationMinutes, item.Version,
            item.AlreadyContains)).ToArray());
    }

    private static async Task<IResult> AddLearningContentAsync(Guid studyPlanId, string slug,
        AddLearningContentToStudyPlanRequest request, AddLearningContentToStudyPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(studyPlanId, slug, request.ExpectedVersion,
            request.SubmissionId), cancellationToken);
        return Results.Ok(new AddLearningContentToStudyPlanResponse(result.StudyPlanId,
            result.ItemId, result.PlanTitle, result.Added, result.Version));
    }

    private static async Task<IResult> ConvertAsync(
        Guid studyPlanId, ConvertStudyPlanRequest request,
        ConvertStudyPlanHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new(studyPlanId, request.ExpectedVersion), cancellationToken);
        return Results.Ok(new ConvertStudyPlanResponse(
            result.StudyPlanId, result.StudyPlanStatus,
            result.StudySessionId, result.StudySessionStatus, result.Title,
            result.ItemCount, result.TotalPlannedDurationMinutes,
            result.ConvertedAtUtc, result.StudyPlanVersion,
            result.StudySessionVersion));
    }

    private static async Task<IResult> UpdateAsync(
        Guid studyPlanId, UpdateStudyPlanRequest request,
        UpdateStudyPlanHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(studyPlanId, request.Title, request.ExpectedVersion),
            cancellationToken)));

    private static async Task<IResult> ReplaceDraftAsync(
        Guid studyPlanId, ReplaceStudyPlanDraftRequest request,
        ReplaceStudyPlanDraftHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(
                studyPlanId, request.Title,
                request.Items.Select(item => new ReplaceStudyPlanDraftItem(
                    item.ItemId, item.ResourceType, item.ResourceId,
                    item.PlannedDurationMinutes)).ToArray(),
                request.ExpectedVersion),
            cancellationToken)));

    private static async Task<IResult> UpdateItemAsync(
        Guid studyPlanId, Guid itemId, UpdateStudyPlanItemRequest request,
        UpdateStudyPlanItemHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(
                studyPlanId, itemId, request.PlannedDurationMinutes,
                request.ExpectedVersion),
            cancellationToken)));

    private static async Task<IResult> RemoveItemAsync(
        Guid studyPlanId, Guid itemId, int expectedVersion,
        RemoveStudyPlanItemHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(studyPlanId, itemId, expectedVersion), cancellationToken)));

    private static async Task<IResult> ReorderItemsAsync(
        Guid studyPlanId, ReorderStudyPlanItemsRequest request,
        ReorderStudyPlanItemsHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(studyPlanId, request.ItemIds, request.ExpectedVersion),
            cancellationToken)));

    private static async Task<IResult> MarkReadyAsync(
        Guid studyPlanId, StudyPlanMutationRequest request,
        MarkStudyPlanReadyHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(studyPlanId, request.ExpectedVersion), cancellationToken)));

    private static async Task<IResult> CancelAsync(
        Guid studyPlanId, StudyPlanMutationRequest request,
        CancelStudyPlanHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(MapMutation(await handler.HandleAsync(
            new(studyPlanId, request.ExpectedVersion), cancellationToken)));

    private static StudyPlanMutationResponse MapMutation(
        StudyPlanMutationResult result) =>
        new(
            result.StudyPlanId, result.Status, result.ItemCount,
            result.TotalPlannedDurationMinutes, result.UpdatedAtUtc,
            result.ReadyAtUtc, result.CancelledAtUtc, result.Version);

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
            item.PlannedDurationMinutes, item.Position, item.ResourceKey, item.ContentType, item.SourceName, item.ResourceKind);
}
