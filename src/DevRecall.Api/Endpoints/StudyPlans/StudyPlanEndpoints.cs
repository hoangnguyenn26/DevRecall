using DevRecall.Api.Authorization;
using DevRecall.Application.StudyPlans.Generate;
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
        return endpoints;
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
}
