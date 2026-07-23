using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge;

public static class KnowledgeHierarchyValidator
{
    public static void EnsureNoCycle(
        Guid nodeId,
        Guid? newParentId,
        IReadOnlyList<KnowledgeNodeHierarchyItem> hierarchy)
    {
        if (newParentId is null)
        {
            return;
        }

        if (newParentId == nodeId)
        {
            ThrowCircularHierarchy();
        }

        var parentById = hierarchy.ToDictionary(
            item => item.Id,
            item => item.ParentId);
        var currentId = newParentId;
        var visited = new HashSet<Guid>();

        while (currentId is not null)
        {
            if (!visited.Add(currentId.Value)
                || currentId.Value == nodeId)
            {
                ThrowCircularHierarchy();
            }

            if (!parentById.TryGetValue(
                currentId.Value,
                out var parentId))
            {
                break;
            }

            currentId = parentId;
        }
    }

    private static void ThrowCircularHierarchy()
    {
        throw new ConflictException(
            KnowledgeErrors.CircularHierarchy.Code,
            KnowledgeErrors.CircularHierarchy.Message);
    }
}
