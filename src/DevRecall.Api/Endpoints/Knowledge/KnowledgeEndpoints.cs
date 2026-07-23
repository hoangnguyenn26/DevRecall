using DevRecall.Api.Authorization;
using DevRecall.Application.Knowledge.Create;
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
}
