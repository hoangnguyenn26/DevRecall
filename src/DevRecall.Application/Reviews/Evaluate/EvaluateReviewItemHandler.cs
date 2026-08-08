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
        ValidateSubmissionId(command.SubmissionId);
        var evaluation = ReviewEvaluationParser.Parse(command.Evaluation);
        var userId = GetCurrentUserId();
        var prior = await reviewHistoryRepository.GetBySubmissionAsync(
            userId, command.SubmissionId, cancellationToken);
        if (prior is not null)
        {
            if (prior.ReviewItemId != command.ReviewItemId)
            {
                throw new ConflictException(
                    ReviewErrors.SubmissionReused.Code,
                    ReviewErrors.SubmissionReused.Message);
            }

            return MapPrior(prior);
        }

        var item = await reviewItemRepository.GetByIdAndUserIdForUpdateAsync(
            command.ReviewItemId, userId, cancellationToken);
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

        var history = ReviewHistory.CreateForSubmission(
            Guid.NewGuid(), userId, command.SubmissionId, item.Id, evaluation,
            schedule, item.ReviewCount, reviewedAtUtc);
        reviewHistoryRepository.Add(history);
        await reviewItemRepository.SaveChangesAsync(cancellationToken);

        return new EvaluateReviewItemResult(
            item.Id, history.Id, evaluation.ToString(),
            schedule.PreviousIntervalDays, schedule.NextIntervalDays,
            schedule.PreviousDueAtUtc, schedule.ReviewedAtUtc,
            schedule.NextDueAtUtc, item.ReviewCount);
    }

    private static EvaluateReviewItemResult MapPrior(
        ReviewSubmissionReadModel prior) => new(
            prior.ReviewItemId, prior.ReviewHistoryId, prior.Evaluation,
            prior.PreviousIntervalDays, prior.NextIntervalDays,
            prior.PreviousDueAtUtc, prior.ReviewedAtUtc,
            prior.NextDueAtUtc, prior.ReviewCount);

    private static void ValidateSubmissionId(Guid submissionId)
    {
        if (submissionId == Guid.Empty)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["submissionId"] = ["Submission id is required."]
                });
        }
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
