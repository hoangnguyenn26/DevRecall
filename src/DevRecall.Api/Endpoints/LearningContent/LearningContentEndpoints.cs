using DevRecall.Api.Authorization;
using DevRecall.Application.LearningContent;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.LearningContent;

namespace DevRecall.Api.Endpoints.LearningContent;

public static class LearningContentEndpoints
{
    public static IEndpointRouteBuilder MapLearningContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/learning-content").WithTags("Learning Content")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapGet("", GetListAsync).WithSummary("Lists published Learning Content.")
            .Produces<PagedResponse<LearningContentListItemResponse>>().ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/{slug}", GetDetailAsync).WithSummary("Gets published Learning Content by slug.")
            .Produces<LearningContentDetailResponse>().ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/{slug}/progress/start", StartAsync).WithSummary("Starts a published lesson idempotently.")
            .Produces<LearningContentProgressResponse>().ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{slug}/progress/complete", CompleteAsync).WithSummary("Completes a published lesson explicitly.")
            .Produces<LearningContentProgressResponse>().ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> GetListAsync([AsParameters] GetLearningContentRequest request,
        GetPublishedLearningContentHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new(request.Technology, request.Topic,
            request.Difficulty, request.Page ?? 1, request.PageSize ?? 20), cancellationToken);
        return Results.Ok(new PagedResponse<LearningContentListItemResponse>(result.Items.Select(Map).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(string slug,
        GetPublishedLearningContentDetailHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(Map(await handler.HandleAsync(slug, cancellationToken)));

    private static async Task<IResult> StartAsync(string slug, StartLearningContentHandler handler,
        CancellationToken cancellationToken) => Results.Ok(Map(await handler.HandleAsync(slug, cancellationToken)));

    private static async Task<IResult> CompleteAsync(string slug, CompleteLearningContentRequest request,
        CompleteLearningContentHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(Map(await handler.HandleAsync(slug, request.ExpectedVersion, cancellationToken)));

    private static LearningContentListItemResponse Map(PublishedLearningContentListItem item) => new(
        item.Slug, item.Title, item.Summary, item.ContentType, item.Difficulty, item.EstimatedMinutes,
        item.Technologies.Select(value => new LearningContentTechnologyResponse(value.Value, value.Label)).ToArray(),
        item.Topics.Select(value => new LearningContentTopicResponse(value.Slug, value.Name)).ToArray(), item.ProgressStatus);

    private static LearningContentDetailResponse Map(PublishedLearningContentDetail item) => new(
        item.Id, item.Slug, item.Title, item.Summary, item.ContentType, item.Difficulty, item.EstimatedMinutes,
        item.Technologies.Select(value => new LearningContentTechnologyResponse(value.Value, value.Label)).ToArray(),
        item.Topics.Select(value => new LearningContentTopicResponse(value.Slug, value.Name)).ToArray(),
        item.Objectives.Select(value => new LearningContentObjectiveResponse(value.Position, value.Text)).ToArray(),
        item.Sections.Select(value => new LearningContentSectionResponse(
            value.Position, value.Type, value.Heading, value.BodyMarkdown)).ToArray(),
        item.ReviewCandidates.Select(value => new LearningContentReviewCandidateResponse(
            value.Key, value.Prompt, value.Answer, value.IsInReview)).ToArray(),
        new(item.Source.Type, item.Source.Name, item.Source.Url), item.PublishedAtUtc, Map(item.Progress));

    private static LearningContentProgressResponse Map(LearningContentProgressItem item) =>
        new(item.Status, item.StartedAtUtc, item.CompletedAtUtc, item.Version, item.CompletionEvidenceId);
}
