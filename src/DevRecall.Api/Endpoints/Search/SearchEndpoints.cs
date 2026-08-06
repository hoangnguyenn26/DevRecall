using DevRecall.Api.Authorization;
using DevRecall.Application.Search;
using DevRecall.Contracts.Search;

namespace DevRecall.Api.Endpoints.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/search", SearchAsync)
            .WithTags("Search")
            .WithName("GlobalSearch")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<GlobalSearchResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] SearchRequest request,
        GlobalSearchHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GlobalSearchQuery(request.Q, request.TakePerType),
            cancellationToken);
        return Results.Ok(new GlobalSearchResponse(
            result.Query,
            result.Items.Select(item => new GlobalSearchResultResponse(
                item.ResourceId, item.ResourceType, item.Title, item.Summary,
                item.TargetPath, item.Rank, item.UpdatedAtUtc,
                item.Highlights.Select(highlight => new SearchHighlightResponse(
                    highlight.Field, highlight.Text)).ToList())).ToList(),
            result.HasMore));
    }
}
