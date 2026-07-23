using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.GetTree;

public sealed class GetKnowledgeTreeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<KnowledgeTreeItem>> HandleAsync(
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        var nodes = await repository.GetActiveByUserIdAsync(
            currentUser.UserId.Value,
            cancellationToken);

        return BuildTree(nodes);
    }

    private static List<KnowledgeTreeItem> BuildTree(
        IReadOnlyList<KnowledgeNode> nodes)
    {
        var childrenLookup = nodes
            .Where(node => node.ParentId is not null)
            .GroupBy(node => node.ParentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(node => node.Title)
                    .ThenBy(node => node.Id)
                    .ToList());

        KnowledgeTreeItem Map(KnowledgeNode node)
        {
            IReadOnlyList<KnowledgeTreeItem> children =
                childrenLookup.TryGetValue(node.Id, out var childNodes)
                    ? childNodes.Select(Map).ToList()
                    : [];

            return new KnowledgeTreeItem(
                node.Id,
                node.ParentId,
                node.Title,
                children);
        }

        return nodes
            .Where(node => node.ParentId is null)
            .OrderBy(node => node.Title)
            .ThenBy(node => node.Id)
            .Select(Map)
            .ToList();
    }
}
