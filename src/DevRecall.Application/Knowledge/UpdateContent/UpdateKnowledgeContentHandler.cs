using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.UpdateContent;

public sealed class UpdateKnowledgeContentHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<UpdateKnowledgeContentResult> HandleAsync(
        UpdateKnowledgeContentCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
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

        if (node.UpdatedAtUtc != command.ExpectedUpdatedAtUtc)
        {
            throw new ConflictException(
                KnowledgeErrors.ConcurrentUpdate.Code,
                KnowledgeErrors.ConcurrentUpdate.Message);
        }

        var changed = node.UpdateContent(command.Content, DateTimeOffset.UtcNow);

        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new UpdateKnowledgeContentResult(node.Id, node.Content, node.UpdatedAtUtc);
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

    private static void Validate(UpdateKnowledgeContentCommand command)
    {
        var errors = new Dictionary<string, string[]>();

        if ((command.Content?.Trim().Length ?? 0) > 100_000)
        {
            errors["content"] = ["Content cannot exceed 100000 characters."];
        }

        if (command.ExpectedUpdatedAtUtc == default)
        {
            errors["expectedUpdatedAtUtc"] = ["Expected updated timestamp is required."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
