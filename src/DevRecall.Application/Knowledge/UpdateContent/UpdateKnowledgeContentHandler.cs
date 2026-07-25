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

        node.UpdateContent(command.Content, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

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
        if ((command.Content?.Trim().Length ?? 0) <= 100_000)
        {
            return;
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["content"] = ["Content cannot exceed 100000 characters."]
        });
    }
}
