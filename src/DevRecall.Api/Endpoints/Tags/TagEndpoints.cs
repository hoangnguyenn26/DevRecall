using DevRecall.Api.Authorization;
using DevRecall.Application.Knowledge.Tags.Archive;
using DevRecall.Application.Knowledge.Tags.Create;
using DevRecall.Application.Knowledge.Tags.GetList;
using DevRecall.Application.Knowledge.Tags.Rename;
using DevRecall.Contracts.Tags;

namespace DevRecall.Api.Endpoints.Tags;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/tags")
            .WithTags("Tags")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateAsync);
        group.MapGet("", GetListAsync);
        group.MapPut("/{id:guid}", RenameAsync);
        group.MapPost("/{id:guid}/archive", ArchiveAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateTagRequest request,
        CreateTagHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateTagCommand(request.Name),
            cancellationToken);

        return Results.Created(
            $"/api/v1/tags/{result.Id}",
            MapResponse(
                result.Id,
                result.Name,
                result.CreatedAtUtc,
                result.UpdatedAtUtc));
    }

    private static async Task<IResult> GetListAsync(
        GetTagsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);

        return Results.Ok(result.Select(tag => MapResponse(
            tag.Id, tag.Name, tag.CreatedAtUtc, tag.UpdatedAtUtc)));
    }

    private static async Task<IResult> RenameAsync(
        Guid id,
        RenameTagRequest request,
        RenameTagHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RenameTagCommand(id, request.Name),
            cancellationToken);

        return Results.Ok(MapResponse(
            result.Id,
            result.Name,
            result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ArchiveTagHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new ArchiveTagCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static TagResponse MapResponse(
        Guid id,
        string name,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, name, createdAtUtc, updatedAtUtc);
}
