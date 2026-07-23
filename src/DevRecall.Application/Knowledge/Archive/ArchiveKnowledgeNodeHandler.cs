using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.Archive;

public sealed class ArchiveKnowledgeNodeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ArchiveKnowledgeNodeCommand command,
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

        if (node.Status == KnowledgeNodeStatus.Archived)
        {
            return;
        }

        node.Archive(DateTimeOffset.UtcNow);
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
