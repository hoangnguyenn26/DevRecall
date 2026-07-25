using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge.GetDetail;

public sealed class GetKnowledgeNodeDetailHandler(
    IKnowledgeNodeRepository repository,
    ICurrentUser currentUser)
{
    public async Task<GetKnowledgeNodeDetailResult> HandleAsync(
        GetKnowledgeNodeDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var node = await repository.GetByIdAndUserIdAsync(query.Id, userId, cancellationToken);

        if (node is null)
        {
            throw new NotFoundException(
                KnowledgeErrors.NodeNotFound.Code,
                KnowledgeErrors.NodeNotFound.Message);
        }

        return new GetKnowledgeNodeDetailResult(
            node.Id,
            node.ParentId,
            node.Title,
            node.Content,
            node.Description,
            node.SourceUrl,
            node.Status.ToString(),
            node.CreatedAtUtc,
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
}
