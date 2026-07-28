using DevRecall.Api.Authorization;
using DevRecall.Application.Dsa.Archive;
using DevRecall.Application.Dsa.Create;
using DevRecall.Application.Dsa.GetDetail;
using DevRecall.Application.Dsa.GetList;
using DevRecall.Application.Dsa.Update;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Dsa;

namespace DevRecall.Api.Endpoints.Dsa;

public static class DsaProblemEndpoints
{
    public static IEndpointRouteBuilder MapDsaProblemEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/dsa-problems")
            .WithTags("DSA Problems")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateAsync);
        group.MapGet("", GetListAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/archive", ArchiveAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateDsaProblemRequest request,
        CreateDsaProblemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateDsaProblemCommand(
                request.Title, request.Description, request.Difficulty,
                request.Source, request.ExternalUrl, request.Topics),
            cancellationToken);
        return Results.Created(
            $"/api/v1/dsa-problems/{result.Id}",
            MapResponse(
                result.Id, result.Title, result.Description,
                result.Difficulty, result.Source, result.ExternalUrl,
                result.Topics, result.Status, result.CreatedAtUtc,
                result.UpdatedAtUtc));
    }

    private static async Task<IResult> GetListAsync(
        [AsParameters] GetDsaProblemsRequest request,
        GetDsaProblemsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDsaProblemsQuery(
                request.Difficulty, request.Topic, request.Source,
                request.Page ?? 1, request.PageSize ?? 20),
            cancellationToken);
        return Results.Ok(new PagedResponse<DsaProblemListItemResponse>(
            result.Items.Select(problem => new DsaProblemListItemResponse(
                problem.Id, problem.Title, problem.Difficulty,
                problem.Source, problem.Topics, problem.UpdatedAtUtc))
                .ToList(),
            result.Page, result.PageSize, result.TotalCount,
            result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        GetDsaProblemDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDsaProblemDetailQuery(id), cancellationToken);
        return Results.Ok(new DsaProblemDetailResponse(
            result.Id, result.Title, result.Description, result.Difficulty,
            result.Source, result.ExternalUrl, result.Topics, result.Status,
            result.CreatedAtUtc, result.UpdatedAtUtc,
            new DsaAttemptSummaryResponse(
                result.AttemptSummary.TotalAttempts,
                result.AttemptSummary.SolvedAttempts,
                result.AttemptSummary.PartiallySolvedAttempts,
                result.AttemptSummary.FailedAttempts,
                result.AttemptSummary.SkippedAttempts,
                result.AttemptSummary.TotalDurationMinutes,
                result.AttemptSummary.AverageDurationMinutes,
                result.AttemptSummary.LastAttemptedAtUtc),
            MapOptionalOverview(result.LatestAttempt),
            MapOptionalOverview(result.LatestSuccessfulAttempt),
            result.RecentAttempts.Select(MapOverview).ToList()));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateDsaProblemRequest request,
        UpdateDsaProblemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateDsaProblemCommand(
                id, request.Title, request.Description, request.Difficulty,
                request.Source, request.ExternalUrl, request.Topics),
            cancellationToken);
        return Results.Ok(MapResponse(
            result.Id, result.Title, result.Description, result.Difficulty,
            result.Source, result.ExternalUrl, result.Topics, result.Status,
            result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ArchiveDsaProblemHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ArchiveDsaProblemCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static DsaProblemResponse MapResponse(
        Guid id, string title, string description, string difficulty,
        string? source, string? externalUrl, IReadOnlyList<string> topics,
        string status, DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id, title, description, difficulty, source, externalUrl, topics,
            status, createdAtUtc, updatedAtUtc);

    private static DsaAttemptOverviewResponse MapOverview(
        DsaAttemptOverview attempt) =>
        new(
            attempt.Id, attempt.AttemptNumber, attempt.Result,
            attempt.Language, attempt.TimeComplexity,
            attempt.SpaceComplexity, attempt.DurationMinutes,
            attempt.AttemptedAtUtc);

    private static DsaAttemptOverviewResponse? MapOptionalOverview(
        DsaAttemptOverview? attempt) =>
        attempt is null ? null : MapOverview(attempt);
}
