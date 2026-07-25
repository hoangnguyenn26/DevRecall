using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.ChangePosition;

public sealed class ChangeKnowledgeNodePositionHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ChangeKnowledgeNodePositionCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRequest(command);
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
            throw new ConflictException(
                KnowledgeErrors.NodeArchived.Code,
                "An archived knowledge node cannot be moved.");
        }

        await ValidateTargetParentAsync(
            node, userId, command.TargetParentId, cancellationToken);
        var hierarchy = await repository.GetHierarchyAsync(
            userId,
            cancellationToken);
        KnowledgeHierarchyValidator.EnsureNoCycle(
            node.Id,
            command.TargetParentId,
            hierarchy);

        if (node.ParentId == command.TargetParentId)
        {
            await ReorderWithinSameParentAsync(
                node, userId, command.TargetIndex, cancellationToken);
            return;
        }

        await MoveToDifferentParentAsync(
            node,
            userId,
            command.TargetParentId,
            command.TargetIndex,
            cancellationToken);
    }

    private async Task ValidateTargetParentAsync(
        KnowledgeNode node,
        Guid userId,
        Guid? targetParentId,
        CancellationToken cancellationToken)
    {
        if (targetParentId is null)
        {
            return;
        }

        if (targetParentId.Value == node.Id)
        {
            throw new ConflictException(
                KnowledgeErrors.CircularHierarchy.Code,
                KnowledgeErrors.CircularHierarchy.Message);
        }

        var targetParent = await repository.GetByIdAsync(
            targetParentId.Value,
            cancellationToken);

        if (targetParent is null || targetParent.UserId != userId)
        {
            throw new NotFoundException(
                KnowledgeErrors.ParentNotFound.Code,
                KnowledgeErrors.ParentNotFound.Message);
        }

        if (targetParent.Status != KnowledgeNodeStatus.Active)
        {
            throw new ConflictException(
                KnowledgeErrors.InvalidParent.Code,
                "An archived knowledge node cannot be used as a parent.");
        }
    }

    private async Task ReorderWithinSameParentAsync(
        KnowledgeNode node,
        Guid userId,
        int targetIndex,
        CancellationToken cancellationToken)
    {
        var siblings = (await repository.GetActiveSiblingsAsync(
            userId, node.ParentId, cancellationToken)).ToList();
        ValidateExistingPosition(node, siblings, targetIndex);
        var currentIndex = siblings.FindIndex(sibling => sibling.Id == node.Id);

        if (currentIndex == targetIndex)
        {
            return;
        }

        siblings.RemoveAt(currentIndex);
        siblings.Insert(targetIndex, node);
        NormalizePositions(siblings, node.ParentId, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task MoveToDifferentParentAsync(
        KnowledgeNode node,
        Guid userId,
        Guid? targetParentId,
        int targetIndex,
        CancellationToken cancellationToken)
    {
        var oldParentId = node.ParentId;
        var oldSiblings = (await repository.GetActiveSiblingsAsync(
                userId, oldParentId, cancellationToken))
            .Where(sibling => sibling.Id != node.Id)
            .ToList();
        var targetSiblings = (await repository.GetActiveSiblingsAsync(
                userId, targetParentId, cancellationToken))
            .Where(sibling => sibling.Id != node.Id)
            .ToList();

        ValidateInsertionIndex(targetIndex, targetSiblings.Count);
        targetSiblings.Insert(targetIndex, node);
        var now = DateTimeOffset.UtcNow;
        NormalizePositions(oldSiblings, oldParentId, now);
        NormalizePositions(targetSiblings, targetParentId, now);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static void NormalizePositions(
        List<KnowledgeNode> nodes,
        Guid? parentId,
        DateTimeOffset updatedAtUtc)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            nodes[index].ChangePosition(parentId, index, updatedAtUtc);
        }
    }

    private static void ValidateExistingPosition(
        KnowledgeNode node,
        List<KnowledgeNode> siblings,
        int targetIndex)
    {
        if (siblings.All(sibling => sibling.Id != node.Id))
        {
            throw new NotFoundException(
                KnowledgeErrors.NodeNotFound.Code,
                KnowledgeErrors.NodeNotFound.Message);
        }

        if (targetIndex < 0 || targetIndex >= siblings.Count)
        {
            ThrowInvalidTargetIndex(allowAppend: false);
        }
    }

    private static void ValidateInsertionIndex(
        int targetIndex,
        int targetSiblingCount)
    {
        if (targetIndex < 0 || targetIndex > targetSiblingCount)
        {
            ThrowInvalidTargetIndex(allowAppend: true);
        }
    }

    private static void ValidateRequest(
        ChangeKnowledgeNodePositionCommand command)
    {
        if (command.TargetIndex < 0)
        {
            ThrowInvalidTargetIndex(allowAppend: true);
        }
    }

    private static void ThrowInvalidTargetIndex(bool allowAppend)
    {
        var message = allowAppend
            ? "Target index must be between zero and the target sibling count."
            : "Target index must refer to an existing sibling position.";

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["targetIndex"] = [message]
        });
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
}
