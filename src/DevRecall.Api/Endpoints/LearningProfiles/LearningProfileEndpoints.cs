using DevRecall.Api.Authorization;
using DevRecall.Application.LearningProfiles;
using DevRecall.Contracts.LearningProfiles;

namespace DevRecall.Api.Endpoints.LearningProfiles;

public static class LearningProfileEndpoints
{
    public static IEndpointRouteBuilder MapLearningProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning-profile").WithTags("Learning Profile")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapGet("/", GetAsync).WithName("GetCurrentLearningProfile")
            .WithSummary("Gets the current user's learning profile.")
            .Produces<LearningProfileResponse>().ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/options", GetOptions).WithName("GetLearningProfileOptions")
            .WithSummary("Gets stable Learning Profile options and display metadata.")
            .Produces<LearningProfileOptionsResponse>();
        group.MapPut("/", PutAsync).WithName("PutCurrentLearningProfile")
            .WithSummary("Creates or updates the current user's complete learning profile.")
            .Produces<LearningProfileResponse>().ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(LearningProfileHandler handler,
        CancellationToken cancellationToken) => Results.Ok(Map(await handler.GetAsync(cancellationToken)));

    private static IResult GetOptions() => Results.Ok(Map(LearningProfileHandler.GetOptions()));

    private static async Task<IResult> PutAsync(PutLearningProfileRequest request,
        LearningProfileHandler handler, CancellationToken cancellationToken) => Results.Ok(Map(
            await handler.PutAsync(new(request.TargetRole, request.ExperienceLevel,
                request.AvailableMinutesPerDay,
                request.Technologies.Select(item => new LearningProfileTechnologyInput(item.Name, item.IsPrimary)).ToArray(),
                request.Goals, request.ExpectedVersion), cancellationToken)));

    private static LearningProfileResponse Map(LearningProfileResult result) => new(
        result.IsConfigured, result.TargetRole is null ? null : Map(result.TargetRole),
        result.ExperienceLevel is null ? null : Map(result.ExperienceLevel),
        result.AvailableMinutesPerDay,
        result.Technologies.Select(item => new LearningProfileTechnologyResponse(
            item.Value, item.Label, item.IsPrimary)).ToArray(),
        result.Goals.Select(Map).ToArray(), result.Version, result.UpdatedAtUtc);

    private static LearningProfileOptionsResponse Map(LearningProfileOptionsResult result) => new(
        result.TargetRoles.Select(Map).ToArray(), result.ExperienceLevels.Select(Map).ToArray(),
        result.TechnologyGroups.Select(group => new LearningProfileTechnologyGroupResponse(
            group.Name, group.Items.Select(Map).ToArray())).ToArray(),
        result.Goals.Select(Map).ToArray(), result.StudyTimeOptions);
    private static LearningProfileOptionResponse Map(LearningProfileOption option) =>
        new(option.Value, option.Label, option.Description);
    private static LearningProfileValueResponse Map(LearningProfileValueResult value) =>
        new(value.Value, value.Label);
}
