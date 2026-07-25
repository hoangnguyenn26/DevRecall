using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.Reorder;

public sealed class ReorderKnowledgeNodeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ReorderKnowledgeNodeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var node = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (node is null || node.UserId != userId)
        {
            throw new NotFoundException(
                KnowledgeErrors.NodeNotFound.Code,
                KnowledgeErrors.NodeNotFound.Message);
        }

        if (node.Status != KnowledgeNodeStatus.Active)
        {
            throw new ConflictException(KnowledgeErrors.NodeArchived.Code, KnowledgeErrors.NodeArchived.Message);
        }

        var siblings = await repository.GetActiveSiblingsAsync(
            userId,
            node.ParentId,
            cancellationToken);
        ValidateTargetIndex(command.TargetIndex, siblings.Count);

        var orderedSiblings = siblings.ToList();
        var currentIndex = orderedSiblings.FindIndex(sibling => sibling.Id == node.Id);

        if (currentIndex < 0)
        {
            throw new NotFoundException(
                KnowledgeErrors.NodeNotFound.Code,
                KnowledgeErrors.NodeNotFound.Message);
        }

        if (currentIndex == command.TargetIndex)
        {
            return;
        }

        orderedSiblings.RemoveAt(currentIndex);
        orderedSiblings.Insert(command.TargetIndex, node);
        var now = DateTimeOffset.UtcNow;
        var changed = false;

        for (var index = 0; index < orderedSiblings.Count; index++)
        {
            changed |= orderedSiblings[index].ChangeSortOrder(index, now);
        }

        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private Guid GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }

    private static void ValidateTargetIndex(int targetIndex, int siblingCount)
    {
        if (targetIndex < 0 || targetIndex >= siblingCount)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["targetIndex"] = ["Target index must refer to an existing sibling position."]
            });
        }
    }
}
