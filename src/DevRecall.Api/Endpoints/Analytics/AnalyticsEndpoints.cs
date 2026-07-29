using DevRecall.Api.Authorization;

namespace DevRecall.Api.Endpoints.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapGroup("/api/v1/analytics")
            .WithTags("Analytics")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        return endpoints;
    }
}
