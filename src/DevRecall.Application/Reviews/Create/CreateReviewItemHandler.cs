using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Create;

public sealed class CreateReviewItemHandler(
    IReviewItemRepository reviewItemRepository,
    IReviewResourceResolver resourceResolver,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<CreateReviewItemResult> HandleAsync(
        CreateReviewItemCommand command, CancellationToken cancellationToken)
    {
        if (command.ResourceId == Guid.Empty)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["resourceId"] = ["Resource id is required."]
                });
        }

        var resourceType = ReviewResourceTypeParser.Parse(command.ResourceType);
        var userId = GetCurrentUserId();
        var resource = await resourceResolver.ResolveAsync(
            userId, resourceType, command.ResourceId, cancellationToken);

        if (resource is null)
        {
            throw new NotFoundException(
                ReviewErrors.ResourceNotFound.Code,
                ReviewErrors.ResourceNotFound.Message);
        }

        if (resource.Availability == ReviewResourceAvailability.Archived)
        {
            throw new ConflictException(
                ReviewErrors.ResourceArchived.Code,
                ReviewErrors.ResourceArchived.Message);
        }

        if (await reviewItemRepository.ActiveExistsAsync(
            userId, resourceType, resource.ResourceId, cancellationToken))
        {
            throw new ConflictException(
                ReviewErrors.ItemAlreadyExists.Code,
                ReviewErrors.ItemAlreadyExists.Message);
        }

        var now = utcClock.UtcNow;
        var item = ReviewItem.Create(
            Guid.NewGuid(), userId, resourceType, resource.ResourceId, now, now);
        reviewItemRepository.Add(item);

        await reviewItemRepository.SaveChangesAsync(cancellationToken);

        return new CreateReviewItemResult(
            item.Id, item.ResourceType.ToString(), item.ResourceId,
            item.Status.ToString(), item.DueAtUtc, item.LastReviewedAtUtc,
            item.IntervalDays, item.ReviewCount, item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    private Guid GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
