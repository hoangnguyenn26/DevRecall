using DevRecall.Api.Authorization;
using DevRecall.Application.Search;
using DevRecall.Contracts.Common;
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
            .Produces<PagedResponse<SearchResultResponse>>()
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
            new GlobalSearchQuery(
                request.Q, request.Modules, request.Page, request.PageSize),
            cancellationToken);
        return Results.Ok(SearchResponse.Create(
            result.Items.Select(item => new SearchResultResponse(
                item.ResourceType, item.ResourceId, item.Title, item.Preview,
                item.Rank, item.Metadata)).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
