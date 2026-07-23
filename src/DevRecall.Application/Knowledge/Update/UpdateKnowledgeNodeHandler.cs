using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.Update;

public sealed class UpdateKnowledgeNodeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<UpdateKnowledgeNodeResult> HandleAsync(
        UpdateKnowledgeNodeCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
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

        node.Rename(command.Title, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return new UpdateKnowledgeNodeResult(
            node.Id,
            node.ParentId,
            node.Title,
            node.UpdatedAtUtc);
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

    private static void Validate(UpdateKnowledgeNodeCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (command.Title.Trim().Length > 200)
        {
            errors["title"] = ["Title cannot exceed 200 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
