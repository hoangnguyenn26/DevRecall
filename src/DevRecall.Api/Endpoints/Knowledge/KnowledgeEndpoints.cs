using DevRecall.Api.Authorization;
using DevRecall.Application.Knowledge.Archive;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Application.Knowledge.GetDetail;
using DevRecall.Application.Knowledge.GetTree;
using DevRecall.Application.Knowledge.Move;
using DevRecall.Application.Knowledge.Update;
using DevRecall.Application.Knowledge.UpdateContent;
using DevRecall.Application.Knowledge.UpdateMetadata;
using DevRecall.Contracts.Knowledge;

namespace DevRecall.Api.Endpoints.Knowledge;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/knowledge-nodes")
            .WithTags("Knowledge")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("", CreateAsync);
        group.MapGet("/tree", GetTreeAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/archive", ArchiveAsync);
        group.MapPut("/{id:guid}/parent", MoveAsync);
        group.MapPut("/{id:guid}/content", UpdateContentAsync);
        group.MapPut("/{id:guid}/metadata", UpdateMetadataAsync);

        return endpoints;
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        GetKnowledgeNodeDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetKnowledgeNodeDetailQuery(id),
            cancellationToken);

        return Results.Ok(new KnowledgeNodeDetailResponse(
            result.Id,
            result.ParentId,
            result.Title,
            result.Content,
            result.Description,
            result.SourceUrl,
            result.Status,
            result.SortOrder,
            result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateKnowledgeNodeRequest request,
        UpdateKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateKnowledgeNodeCommand(id, request.Title),
            cancellationToken);

        return Results.Ok(new UpdateKnowledgeNodeResponse(
            result.Id,
            result.ParentId,
            result.Title,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ArchiveKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ArchiveKnowledgeNodeCommand(id),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> UpdateContentAsync(
        Guid id,
        UpdateKnowledgeContentRequest request,
        UpdateKnowledgeContentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateKnowledgeContentCommand(id, request.Content, request.ExpectedUpdatedAtUtc),
            cancellationToken);

        return Results.Ok(new UpdateKnowledgeContentResponse(
            result.Id,
            result.Content,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> UpdateMetadataAsync(
        Guid id,
        UpdateKnowledgeMetadataRequest request,
        UpdateKnowledgeMetadataHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateKnowledgeMetadataCommand(id, request.Description, request.SourceUrl),
            cancellationToken);

        return Results.Ok(new UpdateKnowledgeMetadataResponse(
            result.Id,
            result.Description,
            result.SourceUrl,
            result.UpdatedAtUtc));
    }

    private static async Task<IResult> MoveAsync(
        Guid id,
        MoveKnowledgeNodeRequest request,
        MoveKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new MoveKnowledgeNodeCommand(id, request.ParentId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> CreateAsync(
        CreateKnowledgeNodeRequest request,
        CreateKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateKnowledgeNodeCommand(request.Title, request.ParentId),
            cancellationToken);

        return Results.Created(
            $"/api/v1/knowledge-nodes/{result.Id}",
            new KnowledgeNodeResponse(
                result.Id,
                result.ParentId,
                result.Title,
                result.SortOrder,
                result.CreatedAtUtc,
                result.UpdatedAtUtc));
    }

    private static async Task<IResult> GetTreeAsync(
        GetKnowledgeTreeHandler handler,
        CancellationToken cancellationToken)
    {
        var tree = await handler.HandleAsync(cancellationToken);

        return Results.Ok(tree.Select(MapTreeNode));
    }

    private static KnowledgeTreeNodeResponse MapTreeNode(
        KnowledgeTreeItem item)
    {
        return new KnowledgeTreeNodeResponse(
            item.Id,
            item.ParentId,
            item.Title,
            item.SortOrder,
            item.Children.Select(MapTreeNode).ToList());
    }
}
