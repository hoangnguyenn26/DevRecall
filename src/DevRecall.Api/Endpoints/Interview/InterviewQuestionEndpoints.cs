using DevRecall.Api.Authorization;
using DevRecall.Application.Interview.Answers.CreateDraft;
using DevRecall.Application.Interview.Answers.Publish;
using DevRecall.Application.Interview.Answers.UpdateDraft;
using DevRecall.Application.Interview.Archive;
using DevRecall.Application.Interview.Create;
using DevRecall.Application.Interview.GetDetail;
using DevRecall.Application.Interview.GetList;
using DevRecall.Application.Interview.Update;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Interview;
using DevRecall.Contracts.Interview.Answers;

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
        group.MapGet("", GetListAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/archive", ArchiveAsync);
        group.MapPost(
            "/{questionId:guid}/answer-versions",
            CreateAnswerDraftAsync);
        group.MapPut(
            "/{questionId:guid}/answer-versions/{versionId:guid}",
            UpdateAnswerDraftAsync);
        group.MapPost(
            "/{questionId:guid}/answer-versions/{versionId:guid}/publish",
            PublishAnswerVersionAsync);
        return endpoints;
    }

    private static async Task<IResult> PublishAnswerVersionAsync(
        Guid questionId,
        Guid versionId,
        PublishInterviewAnswerVersionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new PublishInterviewAnswerVersionCommand(questionId, versionId),
            cancellationToken);

        return Results.Ok(MapAnswerResponse(
            result.Id, result.InterviewQuestionId, result.VersionNumber,
            result.Content, result.Status, result.CreatedAtUtc,
            result.UpdatedAtUtc, result.PublishedAtUtc));
    }

    private static async Task<IResult> CreateAnswerDraftAsync(
        Guid questionId,
        CreateInterviewAnswerDraftRequest request,
        CreateInterviewAnswerDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateInterviewAnswerDraftCommand(questionId, request.Content),
            cancellationToken);

        return Results.Created(
            $"/api/v1/interview-questions/{questionId}/answer-versions/{result.Id}",
            MapAnswerResponse(
                result.Id, result.InterviewQuestionId, result.VersionNumber,
                result.Content, result.Status, result.CreatedAtUtc,
                result.UpdatedAtUtc, result.PublishedAtUtc));
    }

    private static async Task<IResult> UpdateAnswerDraftAsync(
        Guid questionId,
        Guid versionId,
        UpdateInterviewAnswerDraftRequest request,
        UpdateInterviewAnswerDraftHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateInterviewAnswerDraftCommand(
                questionId, versionId, request.Content),
            cancellationToken);

        return Results.Ok(MapAnswerResponse(
            result.Id, result.InterviewQuestionId, result.VersionNumber,
            result.Content, result.Status, result.CreatedAtUtc,
            result.UpdatedAtUtc, result.PublishedAtUtc));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ArchiveInterviewQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ArchiveInterviewQuestionCommand(id),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateInterviewQuestionRequest request,
        UpdateInterviewQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateInterviewQuestionCommand(
                id, request.Title, request.Question, request.Topic,
                request.Difficulty, request.Notes),
            cancellationToken);

        return Results.Ok(new UpdateInterviewQuestionResponse(
            result.Id, result.Title, result.Question, result.Topic,
            result.Difficulty, result.Notes, result.Status,
            result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<IResult> GetListAsync(
        [AsParameters] GetInterviewQuestionsRequest request,
        GetInterviewQuestionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetInterviewQuestionsQuery(
                request.Topic, request.Difficulty,
                request.Page ?? 1, request.PageSize ?? 20),
            cancellationToken);

        return Results.Ok(new PagedResponse<InterviewQuestionListItemResponse>(
            result.Items.Select(question =>
                new InterviewQuestionListItemResponse(
                    question.Id, question.Title, question.Topic,
                    question.Difficulty, question.UpdatedAtUtc))
                .ToList(),
            result.Page, result.PageSize,
            result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        GetInterviewQuestionDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetInterviewQuestionDetailQuery(id),
            cancellationToken);

        return Results.Ok(new InterviewQuestionDetailResponse(
            result.Id, result.Title, result.Question, result.Topic,
            result.Difficulty, result.Notes, result.Status,
            result.CreatedAtUtc, result.UpdatedAtUtc));
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

    private static InterviewAnswerVersionResponse MapAnswerResponse(
        Guid id,
        Guid interviewQuestionId,
        int versionNumber,
        string content,
        string status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? publishedAtUtc) =>
        new(
            id, interviewQuestionId, versionNumber, content, status,
            createdAtUtc, updatedAtUtc, publishedAtUtc);
}
