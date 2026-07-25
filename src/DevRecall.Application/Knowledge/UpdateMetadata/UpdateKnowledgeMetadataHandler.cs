using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.UpdateMetadata;

public sealed class UpdateKnowledgeMetadataHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<UpdateKnowledgeMetadataResult> HandleAsync(
        UpdateKnowledgeMetadataCommand command,
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

        node.UpdateMetadata(command.Description, command.SourceUrl, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);

        return new UpdateKnowledgeMetadataResult(
            node.Id,
            node.Description,
            node.SourceUrl,
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

    private static void Validate(UpdateKnowledgeMetadataCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedDescription = command.Description?.Trim();
        var normalizedSourceUrl = command.SourceUrl?.Trim();

        if (normalizedDescription?.Length > 500)
        {
            errors["description"] = ["Description cannot exceed 500 characters."];
        }

        if (!string.IsNullOrWhiteSpace(normalizedSourceUrl)
            && (normalizedSourceUrl.Length > 2048
                || !Uri.TryCreate(normalizedSourceUrl, UriKind.Absolute, out _)))
        {
            errors["sourceUrl"] =
                ["Source URL must be a valid absolute URL and cannot exceed 2048 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
