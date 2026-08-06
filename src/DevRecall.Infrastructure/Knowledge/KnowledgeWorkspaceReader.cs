using DevRecall.Application.Knowledge.Workspace;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge;

internal sealed class KnowledgeWorkspaceReader(DevRecallDbContext dbContext)
    : IKnowledgeWorkspaceReader
{
    private const int MaximumTreeDepth = 6;

    public Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken) =>
        dbContext.KnowledgeNodes.AsNoTracking().AnyAsync(node =>
            node.Id == topicId && node.UserId == userId && node.Status == KnowledgeNodeStatus.Active,
            cancellationToken);

    public async Task<KnowledgeListReadResult> GetListAsync(
        Guid userId, KnowledgeListFilter filter, CancellationToken cancellationToken)
    {
        var nodes = dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId && node.Status == KnowledgeNodeStatus.Active);

        if (filter.TopicId.HasValue)
        {
            var hierarchy = await nodes.Select(node => new HierarchyRow(node.Id, node.ParentId))
                .ToListAsync(cancellationToken);
            var ids = CollectDescendants(hierarchy, filter.TopicId.Value);
            nodes = nodes.Where(node => ids.Contains(node.Id));
        }
        else if (filter.Uncategorized)
        {
            nodes = nodes.Where(node => node.ParentId == null);
        }

        if (filter.TagIds.Count > 0)
        {
            var requiredCount = filter.TagIds.Count;
            var matchingIds = dbContext.KnowledgeNodeTags.AsNoTracking()
                .Where(relation => filter.TagIds.Contains(relation.TagId))
                .GroupBy(relation => relation.KnowledgeNodeId)
                .Where(group => group.Select(relation => relation.TagId).Distinct().Count() == requiredCount)
                .Select(group => group.Key);
            nodes = nodes.Where(node => matchingIds.Contains(node.Id));
        }

        if (filter.Query is not null)
        {
            nodes = nodes.Where(node => EF.Functions.ToTsVector("simple",
                node.Title + " " + node.Content + " " + (node.Description ?? ""))
                .Matches(EF.Functions.WebSearchToTsQuery("simple", filter.Query)));
        }

        var totalCount = await nodes.CountAsync(cancellationToken);
        var exactPattern = filter.Query is null ? null : EscapeLikePattern(filter.Query);
        var ordered = ApplyOrdering(nodes, filter, exactPattern, exactPattern + "%");
        var pageRows = await (
            from node in ordered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            join parent in dbContext.KnowledgeNodes.AsNoTracking()
                on node.ParentId equals parent.Id into parents
            from parent in parents.DefaultIfEmpty()
            select new ListRow(
                node.Id,
                node.Title,
#pragma warning disable CA1845 // EF Core translates Substring while span APIs cannot appear in expression trees.
                node.Description ?? (node.Content.Length > 240
                    ? node.Content.Substring(0, 240) + "…"
                    : node.Content),
#pragma warning restore CA1845
                node.ParentId,
                parent == null ? null : parent.Title,
                node.CreatedAtUtc,
                node.UpdatedAtUtc,
                node.Version))
            .ToListAsync(cancellationToken);

        var idsOnPage = pageRows.Select(row => row.Id).ToArray();
        var tags = idsOnPage.Length == 0
            ? []
            : await (
                from relation in dbContext.KnowledgeNodeTags.AsNoTracking()
                join tag in dbContext.Tags.AsNoTracking() on relation.TagId equals tag.Id
                where idsOnPage.Contains(relation.KnowledgeNodeId)
                    && tag.UserId == userId && tag.Status == TagStatus.Active
                orderby tag.Name, tag.Id
                select new TagRow(relation.KnowledgeNodeId, tag.Id, tag.Name))
                .ToListAsync(cancellationToken);
        var tagGroups = tags.GroupBy(tag => tag.KnowledgeNodeId).ToDictionary(group => group.Key, group => group.ToList());

        var items = pageRows.Select(row => new KnowledgeListItemReadModel(
            row.Id, row.Title, row.Summary, row.TopicId, row.TopicName,
            tagGroups.GetValueOrDefault(row.Id, []).Take(2)
                .Select(tag => new KnowledgeTagReadModel(tag.Id, tag.Name)).ToList(),
            tagGroups.GetValueOrDefault(row.Id, []).Count,
            row.CreatedAtUtc, row.UpdatedAtUtc, row.Version)).ToList();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)filter.PageSize);
        return new KnowledgeListReadResult(items, filter.Page, filter.PageSize, totalCount, totalPages);
    }

    public async Task<KnowledgeDetailReadModel?> GetDetailAsync(
        Guid userId, Guid knowledgeId, CancellationToken cancellationToken)
    {
        var row = await (
            from node in dbContext.KnowledgeNodes.AsNoTracking()
            join parent in dbContext.KnowledgeNodes.AsNoTracking()
                on node.ParentId equals parent.Id into parents
            from parent in parents.DefaultIfEmpty()
            where node.Id == knowledgeId && node.UserId == userId
                && node.Status == KnowledgeNodeStatus.Active
            select new DetailRow(node.Id, node.Title, node.Content, node.Description,
                node.SourceUrl, node.ParentId, parent == null ? null : parent.Title,
                node.CreatedAtUtc, node.UpdatedAtUtc, node.Version))
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var tags = await (
            from relation in dbContext.KnowledgeNodeTags.AsNoTracking()
            join tag in dbContext.Tags.AsNoTracking() on relation.TagId equals tag.Id
            where relation.KnowledgeNodeId == knowledgeId && tag.UserId == userId
                && tag.Status == TagStatus.Active
            orderby tag.Name, tag.Id
            select new KnowledgeTagReadModel(tag.Id, tag.Name))
            .ToListAsync(cancellationToken);
        var tagIds = tags.Select(tag => tag.Id).ToArray();
        var related = await ReadRelatedAsync(userId, knowledgeId, row.TopicId, tagIds, cancellationToken);
        return new KnowledgeDetailReadModel(row.Id, row.Title, row.Content, row.Description,
            row.SourceUrl, row.TopicId, row.TopicName, tags, related,
            row.CreatedAtUtc, row.UpdatedAtUtc, row.Version);
    }

    public async Task<KnowledgeTopicTreeReadModel> GetTopicTreeAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId && node.Status == KnowledgeNodeStatus.Active)
            .OrderBy(node => node.SortOrder).ThenBy(node => node.Title).ThenBy(node => node.Id)
            .Select(node => new TopicRow(node.Id, node.ParentId, node.Title, node.SortOrder))
            .ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToHashSet();
        var normalized = rows.Select(row => row with { ParentId = row.ParentId.HasValue && ids.Contains(row.ParentId.Value) ? row.ParentId : null }).ToList();
        var children = normalized.GroupBy(row => row.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.ToList());
        var visited = new HashSet<Guid>();
        var roots = BuildChildren(Guid.Empty, 0, children, visited);

        foreach (var orphan in normalized.Where(row => !visited.Contains(row.Id)))
            roots.Add(BuildNode(orphan with { ParentId = null }, 0, children, visited));

        return new KnowledgeTopicTreeReadModel(roots, rows.Count, rows.Count(row => row.ParentId is null));
    }

    private static IOrderedQueryable<KnowledgeNode> ApplyOrdering(
        IQueryable<KnowledgeNode> nodes, KnowledgeListFilter filter,
        string? exactPattern, string? prefixPattern)
    {
        if (filter.Query is not null)
        {
            return nodes.OrderByDescending(node => EF.Functions.ILike(node.Title, exactPattern!, "\\"))
                .ThenByDescending(node => EF.Functions.ILike(node.Title, prefixPattern!, "\\"))
                .ThenByDescending(node => EF.Functions.ToTsVector("simple",
                    node.Title + " " + node.Content + " " + (node.Description ?? ""))
                    .RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", filter.Query)))
                .ThenByDescending(node => node.UpdatedAtUtc).ThenBy(node => node.Id);
        }

#pragma warning disable CA1304, CA1311, CA1862 // PostgreSQL translates ToLower for normalized title ordering.
        return filter.Sort switch
        {
            KnowledgeListSort.Created => nodes.OrderByDescending(node => node.CreatedAtUtc).ThenBy(node => node.Id),
            KnowledgeListSort.Title => nodes.OrderBy(node => node.Title.ToLower()).ThenBy(node => node.Id),
            _ => nodes.OrderByDescending(node => node.UpdatedAtUtc)
                .ThenByDescending(node => node.CreatedAtUtc).ThenBy(node => node.Id)
        };
#pragma warning restore CA1304, CA1311, CA1862
    }

    private async Task<IReadOnlyList<RelatedKnowledgeReadModel>> ReadRelatedAsync(
        Guid userId, Guid knowledgeId, Guid? topicId, Guid[] tagIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId && node.Id != knowledgeId
                && node.Status == KnowledgeNodeStatus.Active)
            .Select(node => new
            {
                node.Id,
                node.Title,
                node.ParentId,
                node.UpdatedAtUtc,
                TopicName = dbContext.KnowledgeNodes.Where(topic => topic.Id == node.ParentId)
                    .Select(topic => topic.Title).FirstOrDefault(),
                SharedTagCount = dbContext.KnowledgeNodeTags.Count(relation =>
                    relation.KnowledgeNodeId == node.Id && tagIds.Contains(relation.TagId)),
                SameTopic = topicId != null && node.ParentId == topicId
            })
            .OrderByDescending(item => item.SharedTagCount * 10 + (item.SameTopic ? 4 : 0))
            .ThenByDescending(item => item.SharedTagCount)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.Id)
            .Take(5)
            .Select(item => new RelatedKnowledgeReadModel(item.Id, item.Title,
                item.TopicName, item.SharedTagCount, item.SameTopic, item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private static Guid[] CollectDescendants(IReadOnlyList<HierarchyRow> rows, Guid rootId)
    {
        var children = rows.GroupBy(row => row.ParentId ?? Guid.Empty)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Id).ToList());
        var result = new HashSet<Guid>();
        var queue = new Queue<(Guid Id, int Depth)>();
        queue.Enqueue((rootId, 0));
        while (queue.TryDequeue(out var current) && result.Add(current.Id))
        {
            if (current.Depth >= MaximumTreeDepth || !children.TryGetValue(current.Id, out var directChildren)) continue;
            foreach (var child in directChildren) queue.Enqueue((child, current.Depth + 1));
        }
        return result.ToArray();
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private static List<KnowledgeTopicReadModel> BuildChildren(
        Guid parentId, int depth, IReadOnlyDictionary<Guid, List<TopicRow>> children, HashSet<Guid> visited)
    {
        if (depth > MaximumTreeDepth || !children.TryGetValue(parentId, out var rows)) return [];
        return rows.Where(row => !visited.Contains(row.Id))
            .Select(row => BuildNode(row, depth, children, visited)).ToList();
    }

    private static KnowledgeTopicReadModel BuildNode(
        TopicRow row, int depth, IReadOnlyDictionary<Guid, List<TopicRow>> children, HashSet<Guid> visited)
    {
        visited.Add(row.Id);
        var childItems = BuildChildren(row.Id, depth + 1, children, visited);
        var descendantCount = childItems.Sum(child => child.TotalKnowledgeCount);
        return new KnowledgeTopicReadModel(row.Id, row.ParentId, row.Name, row.SortOrder,
            1, descendantCount, descendantCount + 1, childItems.Count, childItems);
    }

    private sealed record HierarchyRow(Guid Id, Guid? ParentId);
    private sealed record TopicRow(Guid Id, Guid? ParentId, string Name, int SortOrder);
    private sealed record TagRow(Guid KnowledgeNodeId, Guid Id, string Name);
    private sealed record ListRow(Guid Id, string Title, string Summary, Guid? TopicId,
        string? TopicName, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);
    private sealed record DetailRow(Guid Id, string Title, string Content, string? Description,
        string? SourceUrl, Guid? TopicId, string? TopicName,
        DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, int Version);
}
