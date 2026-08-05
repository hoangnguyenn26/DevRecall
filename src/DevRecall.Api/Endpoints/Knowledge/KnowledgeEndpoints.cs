using DevRecall.Api.Authorization;
using DevRecall.Application.Knowledge.Archive;
using DevRecall.Application.Knowledge.ChangePosition;
using DevRecall.Application.Knowledge.Create;
using DevRecall.Application.Knowledge.GetByTags;
using DevRecall.Application.Knowledge.GetDetail;
using DevRecall.Application.Knowledge.GetTree;
using DevRecall.Application.Knowledge.Move;
using DevRecall.Application.Knowledge.Reorder;
using DevRecall.Application.Knowledge.Tags.Assign;
using DevRecall.Application.Knowledge.Tags.Remove;
using DevRecall.Application.Knowledge.Update;
using DevRecall.Application.Knowledge.UpdateContent;
using DevRecall.Application.Knowledge.UpdateMetadata;
using DevRecall.Application.Knowledge.Workspace;
using DevRecall.Application.Common.Exceptions;
using DevRecall.Contracts.Common;
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
        group.MapGet("/by-tags", GetByTagsAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/archive", ArchiveAsync);
        group.MapPut("/{id:guid}/parent", MoveAsync);
        group.MapPut("/{id:guid}/content", UpdateContentAsync);
        group.MapPut("/{id:guid}/metadata", UpdateMetadataAsync);
        group.MapPut("/{id:guid}/order", ReorderAsync);
        group.MapPut("/{id:guid}/position", ChangePositionAsync);
        group.MapPut("/{nodeId:guid}/tags/{tagId:guid}", AssignTagAsync);
        group.MapDelete("/{nodeId:guid}/tags/{tagId:guid}", RemoveTagAsync);

        var workspace = endpoints.MapGroup("/api/v1/knowledge")
            .WithTags("Knowledge Workspace")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        workspace.MapGet("", GetWorkspaceListAsync);
        workspace.MapGet("/topics/tree", GetWorkspaceTopicTreeAsync);
        workspace.MapGet("/tags", GetWorkspaceTagsAsync);
        workspace.MapPost("/tags", CreateWorkspaceTagAsync);
        workspace.MapGet("/{knowledgeId:guid}", GetWorkspaceDetailAsync);
        workspace.MapPut("/{knowledgeId:guid}", UpdateWorkspaceAsync);
        workspace.MapDelete("/{knowledgeId:guid}", DeleteWorkspaceAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceListAsync(
        [AsParameters] GetKnowledgeRequest request,
        GetKnowledgeListHandler handler,
        CancellationToken cancellationToken)
    {
        var tagIds = ParseTagIds(request.TagIds);
        var result = await handler.HandleAsync(new GetKnowledgeListQuery(
            request.TopicId, request.TopicScope, request.Query, request.Sort,
            request.Page, request.PageSize, tagIds), cancellationToken);
        return Results.Ok(new PagedResponse<KnowledgeWorkspaceListItemResponse>(
            result.Items.Select(MapWorkspaceListItem).ToList(), result.Page,
            result.PageSize, result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> GetWorkspaceDetailAsync(
        Guid knowledgeId, GetKnowledgeDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(knowledgeId, cancellationToken);
        return Results.Ok(new KnowledgeWorkspaceDetailResponse(
            result.Id, result.Title, result.Content, result.Description, result.SourceUrl,
            result.TopicId, result.TopicName, result.Tags.Select(tag =>
                new KnowledgeWorkspaceTagResponse(tag.Id, tag.Name)).ToList(),
            result.RelatedItems.Select(item => new RelatedKnowledgeResponse(
                item.Id, item.Title, item.TopicName, item.SharedTagCount,
                item.SameTopic, item.UpdatedAtUtc)).ToList(),
            result.CreatedAtUtc, result.UpdatedAtUtc, result.Version));
    }

    private static async Task<IResult> UpdateWorkspaceAsync(
        Guid knowledgeId, UpdateKnowledgeRequest request,
        UpdateKnowledgeHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateKnowledgeCommand(
            knowledgeId, request.Title, request.Content, request.TopicId,
            request.TagIds ?? [], request.ExpectedVersion), cancellationToken);
        return Results.Ok(new
        {
            detail = MapWorkspaceDetail(result.Detail),
            result.Changed,
            result.TopicChanged
        });
    }

    private static async Task<IResult> DeleteWorkspaceAsync(
        Guid knowledgeId, int expectedVersion, DeleteKnowledgeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(knowledgeId, expectedVersion, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetWorkspaceTagsAsync(
        string? query, int take, GetKnowledgeTagsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, take == 0 ? 20 : take, cancellationToken);
        return Results.Ok(result.Select(tag => new KnowledgeTagSummaryResponse(
            tag.Id, tag.Name, tag.NormalizedName, tag.KnowledgeCount)));
    }

    private static async Task<IResult> CreateWorkspaceTagAsync(
        CreateKnowledgeTagRequest request, CreateKnowledgeTagHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.Name, cancellationToken);
        return Results.Ok(new KnowledgeTagSummaryResponse(
            result.Id, result.Name, result.NormalizedName, result.KnowledgeCount));
    }

    private static async Task<IResult> GetWorkspaceTopicTreeAsync(
        GetKnowledgeTopicTreeHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Results.Ok(new KnowledgeTopicTreeResponse(
            result.Items.Select(MapTopic).ToList(), result.TotalKnowledgeCount,
            result.UncategorizedCount));
    }

    private static KnowledgeWorkspaceListItemResponse MapWorkspaceListItem(
        KnowledgeListItemReadModel item) => new(
            item.Id, item.Title, item.Summary, item.TopicId, item.TopicName,
            item.Tags.Select(tag => new KnowledgeWorkspaceTagResponse(tag.Id, tag.Name)).ToList(),
            item.TagCount, item.CreatedAtUtc, item.UpdatedAtUtc, item.Version);

    private static KnowledgeWorkspaceDetailResponse MapWorkspaceDetail(
        KnowledgeDetailReadModel result) => new(
            result.Id, result.Title, result.Content, result.Description, result.SourceUrl,
            result.TopicId, result.TopicName,
            result.Tags.Select(tag => new KnowledgeWorkspaceTagResponse(tag.Id, tag.Name)).ToList(),
            result.RelatedItems.Select(item => new RelatedKnowledgeResponse(item.Id,
                item.Title, item.TopicName, item.SharedTagCount, item.SameTopic, item.UpdatedAtUtc)).ToList(),
            result.CreatedAtUtc, result.UpdatedAtUtc, result.Version);

    private static List<Guid> ParseTagIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ids = new List<Guid>();
        foreach (var part in parts)
        {
            if (!Guid.TryParse(part, out var id)) throw new ValidationException(
                new Dictionary<string, string[]> { ["tagIds"] = ["Tag IDs must be valid GUID values."] });
            ids.Add(id);
        }
        return ids.Distinct().Order().ToList();
    }

    private static KnowledgeTopicResponse MapTopic(KnowledgeTopicReadModel topic) => new(
        topic.Id, topic.ParentId, topic.Name, topic.DirectKnowledgeCount,
        topic.DescendantKnowledgeCount, topic.TotalKnowledgeCount, topic.ChildCount,
        topic.Children.Select(MapTopic).ToList());

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
            result.UpdatedAtUtc,
            result.Tags.Select(tag =>
                new KnowledgeNodeTagResponse(tag.Id, tag.Name))
                .ToList()));
    }

    private static async Task<IResult> GetByTagsAsync(
        [AsParameters] GetKnowledgeByTagsRequest request,
        GetKnowledgeByTagsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetKnowledgeByTagsQuery(request.TagIds ?? []),
            cancellationToken);

        return Results.Ok(result.Select(node =>
            new KnowledgeNodeListItemResponse(
                node.Id,
                node.ParentId,
                node.Title,
                node.Description,
                node.SortOrder,
                node.Tags.Select(tag =>
                    new KnowledgeNodeTagResponse(tag.Id, tag.Name))
                    .ToList())));
    }

    private static async Task<IResult> AssignTagAsync(
        Guid nodeId,
        Guid tagId,
        AssignTagToKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new AssignTagToKnowledgeNodeCommand(nodeId, tagId),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveTagAsync(
        Guid nodeId,
        Guid tagId,
        RemoveTagFromKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new RemoveTagFromKnowledgeNodeCommand(nodeId, tagId),
            cancellationToken);

        return Results.NoContent();
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

    private static async Task<IResult> ReorderAsync(
        Guid id,
        ReorderKnowledgeNodeRequest request,
        ReorderKnowledgeNodeHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ReorderKnowledgeNodeCommand(id, request.TargetIndex),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePositionAsync(
        Guid id,
        ChangeKnowledgeNodePositionRequest request,
        ChangeKnowledgeNodePositionHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(
            new ChangeKnowledgeNodePositionCommand(
                id,
                request.TargetParentId,
                request.TargetIndex),
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
