using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.Move;

public sealed class MoveKnowledgeNodeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        MoveKnowledgeNodeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var node = await repository.GetByIdAsync(
            command.Id,
            cancellationToken);

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

        if (command.ParentId is not null)
        {
            var parent = await repository.GetByIdAsync(
                command.ParentId.Value,
                cancellationToken);

            if (parent is null || parent.UserId != userId)
            {
                throw new NotFoundException(
                    KnowledgeErrors.ParentNotFound.Code,
                    KnowledgeErrors.ParentNotFound.Message);
            }

            if (parent.Status != KnowledgeNodeStatus.Active)
            {
                throw new ConflictException(
                    KnowledgeErrors.InvalidParent.Code,
                    "An archived knowledge node cannot be used as a parent.");
            }
        }

        var hierarchy = await repository.GetHierarchyAsync(
            userId,
            cancellationToken);
        KnowledgeHierarchyValidator.EnsureNoCycle(
            node.Id,
            command.ParentId,
            hierarchy);

        node.MoveTo(command.ParentId, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
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
