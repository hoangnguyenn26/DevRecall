using DevRecall.Api.Authorization;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Application.Knowledge.GetTree;
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

        return endpoints;
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
            item.Children.Select(MapTreeNode).ToList());
    }
}
