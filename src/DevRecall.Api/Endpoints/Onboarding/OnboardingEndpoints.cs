using DevRecall.Api.Authorization;
using DevRecall.Application.Identity.Onboarding;
using DevRecall.Contracts.Onboarding;

namespace DevRecall.Api.Endpoints.Onboarding;

public static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapGet("/", GetAsync).WithName("GetOnboarding")
            .Produces<GetOnboardingResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/complete", CompleteAsync).WithName("CompleteOnboarding")
            .Produces<GetOnboardingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/skip", SkipAsync).WithName("SkipOnboarding")
            .Produces<GetOnboardingResponse>()
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        OnboardingHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(Map(await handler.GetAsync(cancellationToken)));

    private static async Task<IResult> CompleteAsync(
        CompleteOnboardingRequest request, OnboardingHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(Map(await handler.CompleteAsync(new(
            request.Goal, request.DailyCommitmentMinutes,
            request.WeeklyTargetDays, request.FocusAreas), cancellationToken)));

    private static async Task<IResult> SkipAsync(
        OnboardingHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(Map(await handler.SkipAsync(cancellationToken)));

    private static GetOnboardingResponse Map(GetOnboardingResult result) =>
        new(result.HasCompleted, result.CompletionType, result.Goal,
            result.DailyCommitmentMinutes, result.WeeklyTargetDays,
            result.FocusAreas);
}
