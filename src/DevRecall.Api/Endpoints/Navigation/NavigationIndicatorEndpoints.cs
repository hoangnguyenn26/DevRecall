using DevRecall.Api.Authorization;
using DevRecall.Application.Navigation;
using DevRecall.Contracts.Navigation;

namespace DevRecall.Api.Endpoints.Navigation;

public static class NavigationIndicatorEndpoints
{
    public static IEndpointRouteBuilder MapNavigationIndicatorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/navigation-indicators", async (
            GetNavigationIndicatorsHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken);
            return Results.Ok(new NavigationIndicatorsResponse(
                result.ReviewsDue, result.HasActiveStudyPlan,
                result.CriticalWeakTopics, result.HasIncompleteOnboarding));
        }).WithTags("Navigation").RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Produces<NavigationIndicatorsResponse>().ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }
}
