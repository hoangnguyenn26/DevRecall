using DevRecall.Api.Authorization;
using DevRecall.Application.Dsa.Attempts.Compare;
using DevRecall.Application.Dsa.Attempts.Create;
using DevRecall.Application.Dsa.Attempts.GetDetail;
using DevRecall.Application.Dsa.Attempts.GetLatestSuccessful;
using DevRecall.Application.Dsa.Attempts.GetList;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa.Attempts;

namespace DevRecall.Api.Endpoints.Dsa;

public static class DsaAttemptEndpoints
{
    public static IEndpointRouteBuilder MapDsaAttemptEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/dsa-problems/{problemId:guid}/attempts")
            .WithTags("DSA Attempts")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateAsync);
        group.MapGet("", GetListAsync);
        group.MapGet("/latest-successful", GetLatestSuccessfulAsync);
        group.MapGet("/compare", CompareAsync);
        group.MapGet("/{attemptId:guid}", GetDetailAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid problemId,
        CreateDsaAttemptRequest request,
        CreateDsaAttemptHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateDsaAttemptCommand(
                problemId, request.Result, request.Language,
                request.SolutionCode, request.Approach,
                request.TimeComplexity, request.SpaceComplexity,
                request.DurationMinutes, request.Notes,
                request.AttemptedAtUtc),
            cancellationToken);
        return Results.Created(
            $"/api/v1/dsa-problems/{problemId}/attempts/{result.Id}",
            MapResponse(
                result.Id, result.DsaProblemId, result.AttemptNumber,
                result.Result, result.Language, result.SolutionCode,
                result.Approach, result.TimeComplexity,
                result.SpaceComplexity, result.DurationMinutes, result.Notes,
                result.AttemptedAtUtc, result.CreatedAtUtc));
    }

    private static async Task<IResult> GetListAsync(
        Guid problemId,
        [AsParameters] GetDsaAttemptsRequest request,
        GetDsaAttemptsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDsaAttemptsQuery(
                problemId, request.Result, request.Page ?? 1,
                request.PageSize ?? 20),
            cancellationToken);
        return Results.Ok(new PagedResponse<DsaAttemptListItemResponse>(
            result.Items.Select(attempt =>
                new DsaAttemptListItemResponse(
                    attempt.Id, attempt.AttemptNumber, attempt.Result,
                    attempt.Language, attempt.TimeComplexity,
                    attempt.SpaceComplexity, attempt.DurationMinutes,
                    attempt.AttemptedAtUtc, attempt.CreatedAtUtc)).ToList(),
            result.Page, result.PageSize, result.TotalCount,
            result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid problemId,
        Guid attemptId,
        GetDsaAttemptDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDsaAttemptDetailQuery(problemId, attemptId),
            cancellationToken);
        return Results.Ok(MapResponse(result));
    }

    private static async Task<IResult> GetLatestSuccessfulAsync(
        Guid problemId,
        GetLatestSuccessfulDsaAttemptHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLatestSuccessfulDsaAttemptQuery(problemId),
            cancellationToken);
        return result is null
            ? Results.NoContent()
            : Results.Ok(MapResponse(result));
    }

    private static async Task<IResult> CompareAsync(
        Guid problemId,
        [AsParameters] CompareDsaAttemptsRequest request,
        CompareDsaAttemptsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CompareDsaAttemptsQuery(
                problemId, request.LeftAttemptId, request.RightAttemptId),
            cancellationToken);
        return Results.Ok(new CompareDsaAttemptsResponse(
            result.DsaProblemId,
            MapComparisonSnapshot(result.Left),
            MapComparisonSnapshot(result.Right),
            new DsaAttemptComparisonDifferenceResponse(
                result.Difference.AttemptNumberDifference,
                result.Difference.DurationDifferenceMinutes,
                result.Difference.ResultTransition,
                result.Difference.ResultChanged,
                result.Difference.LanguageChanged,
                result.Difference.SolutionCodeChanged,
                result.Difference.ApproachChanged,
                result.Difference.TimeComplexityChanged,
                result.Difference.SpaceComplexityChanged,
                result.Difference.NotesChanged)));
    }

    private static DsaAttemptComparisonSnapshotResponse MapComparisonSnapshot(
        DsaAttemptComparisonSnapshot attempt) =>
        new(
            attempt.Id, attempt.AttemptNumber, attempt.Result,
            attempt.Language, attempt.SolutionCode, attempt.Approach,
            attempt.TimeComplexity, attempt.SpaceComplexity,
            attempt.DurationMinutes, attempt.Notes, attempt.AttemptedAtUtc,
            attempt.CreatedAtUtc);

    private static DsaAttemptResponse MapResponse(
        GetDsaAttemptDetailResult result) =>
        MapResponse(
            result.Id, result.DsaProblemId, result.AttemptNumber,
            result.Result, result.Language, result.SolutionCode,
            result.Approach, result.TimeComplexity, result.SpaceComplexity,
            result.DurationMinutes, result.Notes, result.AttemptedAtUtc,
            result.CreatedAtUtc);

    private static DsaAttemptResponse MapResponse(
        Guid id, Guid dsaProblemId, int attemptNumber, string result,
        string? language, string? solutionCode, string? approach,
        string? timeComplexity, string? spaceComplexity, int durationMinutes,
        string? notes, DateTimeOffset attemptedAtUtc,
        DateTimeOffset createdAtUtc) =>
        new(
            id, dsaProblemId, attemptNumber, result, language, solutionCode,
            approach, timeComplexity, spaceComplexity, durationMinutes, notes,
            attemptedAtUtc, createdAtUtc);
}
