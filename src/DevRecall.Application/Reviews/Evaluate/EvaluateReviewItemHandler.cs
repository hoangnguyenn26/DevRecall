using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Evaluate;

public sealed class EvaluateReviewItemHandler(
    IReviewItemRepository reviewItemRepository,
    IReviewHistoryRepository reviewHistoryRepository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<EvaluateReviewItemResult> HandleAsync(
        EvaluateReviewItemCommand command, CancellationToken cancellationToken)
    {
        ValidateExpectedReviewCount(command.ExpectedReviewCount);
        var evaluation = ReviewEvaluationParser.Parse(command.Evaluation);
        var item = await reviewItemRepository.GetByIdAndUserIdForUpdateAsync(
            command.ReviewItemId, GetCurrentUserId(), cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(
                ReviewErrors.ItemNotFound.Code,
                ReviewErrors.ItemNotFound.Message);
        }

        var reviewedAtUtc = utcClock.UtcNow;
        ReviewSchedule schedule;
        try
        {
            schedule = item.Evaluate(
                evaluation, command.ExpectedReviewCount, reviewedAtUtc);
        }
        catch (ReviewItemArchivedException)
        {
            throw new ConflictException(
                ReviewErrors.ItemArchived.Code,
                ReviewErrors.ItemArchived.Message);
        }
        catch (ReviewScheduleConflictException)
        {
            throw new ConflictException(
                ReviewErrors.ScheduleConflict.Code,
                ReviewErrors.ScheduleConflict.Message);
        }

        var history = ReviewHistory.Create(
            Guid.NewGuid(), item.Id, evaluation, schedule, reviewedAtUtc);
        reviewHistoryRepository.Add(history);
        await reviewItemRepository.SaveChangesAsync(cancellationToken);

        return new EvaluateReviewItemResult(
            item.Id, history.Id, evaluation.ToString(),
            schedule.PreviousIntervalDays, schedule.NextIntervalDays,
            schedule.PreviousDueAtUtc, schedule.ReviewedAtUtc,
            schedule.NextDueAtUtc, item.ReviewCount);
    }

    private static void ValidateExpectedReviewCount(int expectedReviewCount)
    {
        if (expectedReviewCount < 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["expectedReviewCount"] =
                        ["Expected review count cannot be negative."]
                });
        }
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
