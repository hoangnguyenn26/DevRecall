using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Knowledge.Workspace;

public enum KnowledgeListSort
{
    Updated,
    Created,
    Title
}

public sealed record GetKnowledgeListQuery(
    Guid? TopicId, string? TopicScope, string? Query, string? Sort,
    int Page = 1, int PageSize = 30, IReadOnlyCollection<Guid>? TagIds = null);

public sealed record KnowledgeListFilter(
    Guid? TopicId, bool Uncategorized, string? Query,
    KnowledgeListSort Sort, int Page, int PageSize, IReadOnlyCollection<Guid> TagIds);

public sealed record KnowledgeTagReadModel(Guid Id, string Name);

public sealed record KnowledgeListItemReadModel(
    Guid Id, string Title, string Summary, Guid? TopicId, string? TopicName,
    IReadOnlyList<KnowledgeTagReadModel> Tags,
    int TagCount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);

public sealed record KnowledgeListReadResult(
    IReadOnlyList<KnowledgeListItemReadModel> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record KnowledgeDetailReadModel(
    Guid Id, string Title, string Content, string? Description, string? SourceUrl,
    Guid? TopicId, string? TopicName, IReadOnlyList<KnowledgeTagReadModel> Tags,
    IReadOnlyList<RelatedKnowledgeReadModel> RelatedItems,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);

public sealed record RelatedKnowledgeReadModel(
    Guid Id, string Title, string? TopicName, int SharedTagCount,
    bool SameTopic, DateTimeOffset UpdatedAtUtc);

public sealed record KnowledgeTopicReadModel(
    Guid Id, Guid? ParentId, string Name, int SortOrder,
    int DirectKnowledgeCount, int DescendantKnowledgeCount,
    int TotalKnowledgeCount, int ChildCount,
    IReadOnlyList<KnowledgeTopicReadModel> Children);

public sealed record KnowledgeTopicTreeReadModel(
    IReadOnlyList<KnowledgeTopicReadModel> Items,
    int TotalKnowledgeCount, int UncategorizedCount);

public interface IKnowledgeWorkspaceReader
{
    Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);
    Task<KnowledgeListReadResult> GetListAsync(Guid userId, KnowledgeListFilter filter, CancellationToken cancellationToken);
    Task<KnowledgeDetailReadModel?> GetDetailAsync(Guid userId, Guid knowledgeId, CancellationToken cancellationToken);
    Task<KnowledgeTopicTreeReadModel> GetTopicTreeAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record KnowledgeForUpdate(
    DevRecall.Domain.Knowledge.KnowledgeNode Node, IReadOnlyCollection<Guid> TagIds);

public interface IKnowledgeWorkspaceWriter
{
    Task<KnowledgeForUpdate?> GetForUpdateAsync(Guid userId, Guid knowledgeId, CancellationToken cancellationToken);
    Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken);
    Task<int> CountAvailableTagsAsync(Guid userId, IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(Guid userId, CancellationToken cancellationToken);
    void ReplaceTags(Guid knowledgeId, IReadOnlyCollection<Guid> currentTagIds, IReadOnlyCollection<Guid> tagIds, DateTimeOffset currentUtc);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record UpdateKnowledgeCommand(
    Guid KnowledgeId, string Title, string Content, Guid? TopicId,
    IReadOnlyCollection<Guid> TagIds, int ExpectedVersion);

public sealed class UpdateKnowledgeHandler(
    IKnowledgeWorkspaceWriter writer, IKnowledgeWorkspaceReader reader,
    ICurrentUser currentUser)
{
    public async Task<(KnowledgeDetailReadModel Detail, bool Changed, bool TopicChanged)> HandleAsync(
        UpdateKnowledgeCommand command, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var errors = new Dictionary<string, string[]>();
        if (command.ExpectedVersion < 1) errors["expectedVersion"] = ["Expected version must be greater than zero."];
        var tagIds = command.TagIds.Distinct().Order().ToArray();
        if (tagIds.Length > 10) errors["tagIds"] = ["A knowledge item can have at most 10 tags."];
        if (errors.Count > 0) throw new ValidationException(errors);

        var state = await writer.GetForUpdateAsync(userId, command.KnowledgeId, cancellationToken);
        if (state is null || state.Node.Status != DevRecall.Domain.Knowledge.KnowledgeNodeStatus.Active)
            throw new NotFoundException("KNOWLEDGE_NOT_FOUND", "The knowledge item was not found.");
        if (state.Node.Version != command.ExpectedVersion)
            throw new ConcurrencyException("KNOWLEDGE_CONFLICT", "The knowledge item changed since it was loaded.");

        if (command.TopicId.HasValue)
        {
            if (!await writer.TopicExistsAsync(userId, command.TopicId.Value, cancellationToken))
                throw new NotFoundException("KNOWLEDGE_TOPIC_NOT_FOUND", "The selected knowledge topic was not found.");
            var hierarchy = await writer.GetHierarchyAsync(userId, cancellationToken);
            KnowledgeHierarchyValidator.EnsureNoCycle(state.Node.Id, command.TopicId, hierarchy);
        }
        if (tagIds.Length > 0 && await writer.CountAvailableTagsAsync(userId, tagIds, cancellationToken) != tagIds.Length)
            throw new NotFoundException("KNOWLEDGE_TAG_NOT_FOUND", "One or more selected tags are not available.");

        var topicChanged = state.Node.ParentId != command.TopicId;
        var changed = state.Node.Update(command.Title, command.Content, command.TopicId,
            state.TagIds, tagIds, DateTimeOffset.UtcNow);
        if (changed)
        {
            writer.ReplaceTags(state.Node.Id, state.TagIds, tagIds, state.Node.UpdatedAtUtc);
            await writer.SaveChangesAsync(cancellationToken);
        }
        var detail = await reader.GetDetailAsync(userId, state.Node.Id, cancellationToken)
            ?? throw new NotFoundException("KNOWLEDGE_NOT_FOUND", "The knowledge item was not found.");
        return (detail, changed, topicChanged);
    }

    private Guid GetUserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue
        ? currentUser.UserId.Value
        : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
}

public sealed class DeleteKnowledgeHandler(
    IKnowledgeWorkspaceWriter writer, ICurrentUser currentUser)
{
    public async Task HandleAsync(Guid knowledgeId, int expectedVersion, CancellationToken cancellationToken)
    {
        if (expectedVersion < 1) throw new ValidationException(new Dictionary<string, string[]>
        { ["expectedVersion"] = ["Expected version must be greater than zero."] });
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        var state = await writer.GetForUpdateAsync(userId, knowledgeId, cancellationToken);
        if (state is null || state.Node.Status != DevRecall.Domain.Knowledge.KnowledgeNodeStatus.Active)
            throw new NotFoundException("KNOWLEDGE_NOT_FOUND", "The knowledge item was not found.");
        if (state.Node.Version != expectedVersion)
            throw new ConcurrencyException("KNOWLEDGE_CONFLICT", "The knowledge item changed since it was loaded.");
        state.Node.Archive(DateTimeOffset.UtcNow);
        await writer.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetKnowledgeListHandler(
    IKnowledgeWorkspaceReader reader, ICurrentUser currentUser)
{
    public async Task<KnowledgeListReadResult> HandleAsync(
        GetKnowledgeListQuery query, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (query.TopicId.HasValue && string.Equals(query.TopicScope, "uncategorized", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("KNOWLEDGE_INVALID_TOPIC_FILTER", "topicId and the uncategorized topic scope cannot be combined.");
        }

        var errors = new Dictionary<string, string[]>();
        var search = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        if (search is { Length: > 100 }) errors["query"] = ["Query cannot exceed 100 characters."];
        if (query.Page < 1) errors["page"] = ["Page must be at least 1."];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (!TryParseSort(query.Sort, out var sort)) errors["sort"] = ["Sort must be updated, created, or title."];
        var scope = query.TopicScope?.Trim();
        if (scope is not null && !string.Equals(scope, "uncategorized", StringComparison.OrdinalIgnoreCase))
            errors["topicScope"] = ["Topic scope must be uncategorized."];
        if (errors.Count > 0) throw new ValidationException(errors);

        if (query.TopicId.HasValue && !await reader.TopicExistsAsync(userId, query.TopicId.Value, cancellationToken))
            throw new NotFoundException("KNOWLEDGE_TOPIC_NOT_FOUND", "The selected knowledge topic was not found.");

        var tagIds = query.TagIds?.Distinct().Order().ToArray() ?? [];
        if (tagIds.Length > 10) throw new ValidationException(new Dictionary<string, string[]>
        { ["tagIds"] = ["At most 10 tag filters are allowed."] });
        return await reader.GetListAsync(userId, new KnowledgeListFilter(
            query.TopicId, scope is not null, search, sort, query.Page, query.PageSize, tagIds), cancellationToken);
    }

    private Guid GetUserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue
        ? currentUser.UserId.Value
        : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");

    private static bool TryParseSort(string? value, out KnowledgeListSort sort)
    {
        sort = KnowledgeListSort.Updated;
        if (string.IsNullOrWhiteSpace(value)) return true;
        return value.Trim().ToLowerInvariant() switch
        {
            "updated" => true,
            "created" => (sort = KnowledgeListSort.Created) == KnowledgeListSort.Created,
            "title" => (sort = KnowledgeListSort.Title) == KnowledgeListSort.Title,
            _ => false
        };
    }
}

public sealed class GetKnowledgeDetailHandler(
    IKnowledgeWorkspaceReader reader, ICurrentUser currentUser)
{
    public async Task<KnowledgeDetailReadModel> HandleAsync(Guid knowledgeId, CancellationToken cancellationToken)
    {
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        return await reader.GetDetailAsync(userId, knowledgeId, cancellationToken)
            ?? throw new NotFoundException("KNOWLEDGE_NOT_FOUND", "The knowledge item was not found.");
    }
}

public sealed class GetKnowledgeTopicTreeHandler(
    IKnowledgeWorkspaceReader reader, ICurrentUser currentUser)
{
    public Task<KnowledgeTopicTreeReadModel> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        return reader.GetTopicTreeAsync(userId, cancellationToken);
    }
}
