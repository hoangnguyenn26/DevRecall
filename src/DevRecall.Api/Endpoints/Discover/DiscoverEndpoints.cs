using DevRecall.Api.Authorization;
using DevRecall.Application.Discover;
using DevRecall.Contracts.Discover;

namespace DevRecall.Api.Endpoints.Discover;

public static class DiscoverEndpoints
{
    public static IEndpointRouteBuilder MapDiscoverEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/discover", GetAsync).WithTags("Discover")
            .WithSummary("Lists explainable, deterministically ranked learning suggestions without persisting recommendations.")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<DiscoverResponse>().ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(GetDiscoverHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Results.Ok(new DiscoverResponse(result.ProfileConfigured,
            result.BasedOnGoals.Select(Map).ToArray(), result.BasedOnWeakTopics.Select(Map).ToArray(), result.Recommended.Select(Map).ToArray())
        {
            TrustedResources = result.TrustedResources.Select(item => new DiscoverResourceResponse(item.Slug, item.Title,
                item.Summary, item.ResourceKind, item.SourceName, item.Difficulty, item.EstimatedMinutes,
                item.Technologies.Select(value => new DevRecall.Contracts.LearningContent.LearningContentTechnologyResponse(value.Value, value.Label)).ToArray(),
                item.Topics.Select(value => new DevRecall.Contracts.LearningContent.LearningContentTopicResponse(value.Slug, value.Name)).ToArray(),
                item.Reasons.Select(reason => new DiscoverReasonResponse(reason.Type,
                    reason.Goal is null ? null : new(reason.Goal.Value, reason.Goal.Label), reason.Value, reason.Label, reason.AvailableMinutes)).ToArray())).ToArray()
        });
    }
    private static DiscoverLessonResponse Map(DiscoverLesson item) => new(item.Slug, item.Title, item.Summary,
        item.Difficulty, item.EstimatedMinutes,
        item.Technologies.Select(value => new DevRecall.Contracts.LearningContent.LearningContentTechnologyResponse(value.Value, value.Label)).ToArray(),
        item.Topics.Select(value => new DevRecall.Contracts.LearningContent.LearningContentTopicResponse(value.Slug, value.Name)).ToArray(),
        item.Reasons.Select(reason => new DiscoverReasonResponse(reason.Type,
            reason.Goal is null ? null : new(reason.Goal.Value, reason.Goal.Label),
            reason.Value, reason.Label, reason.AvailableMinutes)).ToArray());
}
