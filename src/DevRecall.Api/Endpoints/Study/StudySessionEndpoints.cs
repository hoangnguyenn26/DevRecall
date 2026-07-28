using DevRecall.Api.Authorization;
using DevRecall.Application.Study;
using DevRecall.Application.Study.Create;
using DevRecall.Application.Study.Items.Add;
using DevRecall.Application.Study.Items.Complete;
using DevRecall.Application.Study.Items.Remove;
using DevRecall.Application.Study.Items.Reorder;
using DevRecall.Application.Study.Items.Skip;
using DevRecall.Application.Study.Items.Start;
using DevRecall.Application.Study.Start;
using DevRecall.Application.Study.Update;
using DevRecall.Contracts.Study;

namespace DevRecall.Api.Endpoints.Study;

public static class StudySessionEndpoints
{
    public static IEndpointRouteBuilder MapStudySessionEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/study-sessions")
            .WithTags("Study Sessions")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/items", AddItemAsync);
        group.MapDelete("/{id:guid}/items/{itemId:guid}", RemoveItemAsync);
        group.MapPut("/{id:guid}/items/reorder", ReorderItemsAsync);
        group.MapPost("/{id:guid}/start", StartSessionAsync);
        group.MapPost(
            "/{id:guid}/items/{itemId:guid}/start", StartItemAsync);
        group.MapPost(
            "/{id:guid}/items/{itemId:guid}/complete", CompleteItemAsync);
        group.MapPost(
            "/{id:guid}/items/{itemId:guid}/skip", SkipItemAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateStudySessionRequest request,
        CreateStudySessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateStudySessionCommand(
                request.Title, request.PlannedDurationMinutes, request.Notes),
            cancellationToken);
        return Results.Created(
            $"/api/v1/study-sessions/{result.Id}", MapSession(result));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateStudySessionRequest request,
        UpdateStudySessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateStudySessionCommand(
                id, request.Title, request.PlannedDurationMinutes,
                request.Notes),
            cancellationToken);
        return Results.Ok(MapSession(result));
    }

    private static async Task<IResult> AddItemAsync(
        Guid id, AddStudySessionItemRequest request,
        AddStudySessionItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddStudySessionItemCommand(
                id, request.ResourceType, request.ResourceId, request.Notes),
            cancellationToken);
        return Results.Created(
            $"/api/v1/study-sessions/{id}/items/{result.Id}",
            MapItem(result));
    }

    private static async Task<IResult> RemoveItemAsync(
        Guid id, Guid itemId,
        RemoveStudySessionItemHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new RemoveStudySessionItemCommand(id, itemId),
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ReorderItemsAsync(
        Guid id, ReorderStudySessionItemsRequest request,
        ReorderStudySessionItemsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReorderStudySessionItemsCommand(id, request.OrderedItemIds),
            cancellationToken);
        return Results.Ok(new ReorderStudySessionItemsResponse(
            result.Select(item => new StudySessionItemPositionResponse(
                item.Id, item.Position)).ToList()));
    }

    private static async Task<IResult> StartSessionAsync(
        Guid id, StartStudySessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StartStudySessionCommand(id), cancellationToken);
        return Results.Ok(new StartStudySessionResponse(
            result.Id, result.Status, result.StartedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> StartItemAsync(
        Guid id, Guid itemId,
        StartStudySessionItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StartStudySessionItemCommand(id, itemId), cancellationToken);
        return Results.Ok(MapItemState(result));
    }

    private static async Task<IResult> CompleteItemAsync(
        Guid id, Guid itemId, CompleteStudySessionItemRequest request,
        CompleteStudySessionItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CompleteStudySessionItemCommand(id, itemId, request.Notes),
            cancellationToken);
        return Results.Ok(MapItemState(result));
    }

    private static async Task<IResult> SkipItemAsync(
        Guid id, Guid itemId, SkipStudySessionItemRequest request,
        SkipStudySessionItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SkipStudySessionItemCommand(id, itemId, request.Notes),
            cancellationToken);
        return Results.Ok(MapItemState(result));
    }

    private static StudySessionResponse MapSession(StudySessionResult result) =>
        new(
            result.Id, result.Title, result.Status,
            result.PlannedDurationMinutes, result.StartedAtUtc,
            result.CompletedAtUtc, result.ActualDurationMinutes,
            result.Notes, result.CreatedAtUtc, result.UpdatedAtUtc);

    private static StudySessionItemResponse MapItem(
        StudySessionItemResult result) =>
        new(
            result.Id, result.ResourceType, result.ResourceId,
            result.Position, result.Status, result.StartedAtUtc,
            result.CompletedAtUtc, result.Notes, result.CreatedAtUtc,
            result.UpdatedAtUtc);

    private static StudySessionItemStateResponse MapItemState(
        StudySessionItemStateResult result) =>
        new(
            result.Id, result.Status, result.StartedAtUtc,
            result.CompletedAtUtc, result.Notes, result.UpdatedAtUtc);
}
