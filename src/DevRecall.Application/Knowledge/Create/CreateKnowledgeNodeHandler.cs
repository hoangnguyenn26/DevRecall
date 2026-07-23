using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.Create;

public sealed class CreateKnowledgeNodeHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<CreateKnowledgeNodeResult> HandleAsync(
        CreateKnowledgeNodeCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        var userId = currentUser.UserId.Value;

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

        var now = DateTimeOffset.UtcNow;
        var node = KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            command.ParentId,
            command.Title,
            now);

        repository.Add(node);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateKnowledgeNodeResult(
            node.Id,
            node.ParentId,
            node.Title,
            node.CreatedAtUtc,
            node.UpdatedAtUtc);
    }

    private static void Validate(CreateKnowledgeNodeCommand command)
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
