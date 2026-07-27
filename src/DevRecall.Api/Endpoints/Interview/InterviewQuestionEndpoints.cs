using DevRecall.Api.Authorization;
using DevRecall.Application.Interview.Create;
using DevRecall.Contracts.Interview;

namespace DevRecall.Api.Endpoints.Interview;

public static class InterviewQuestionEndpoints
{
    public static IEndpointRouteBuilder MapInterviewQuestionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/interview-questions")
            .WithTags("Interview Questions")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateInterviewQuestionRequest request,
        CreateInterviewQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateInterviewQuestionCommand(
                request.Title, request.Question, request.Topic,
                request.Difficulty, request.Notes),
            cancellationToken);

        return Results.Created(
            $"/api/v1/interview-questions/{result.Id}",
            new CreateInterviewQuestionResponse(
                result.Id, result.Title, result.Question, result.Topic,
                result.Difficulty, result.Notes, result.Status,
                result.CreatedAtUtc, result.UpdatedAtUtc));
    }
}
