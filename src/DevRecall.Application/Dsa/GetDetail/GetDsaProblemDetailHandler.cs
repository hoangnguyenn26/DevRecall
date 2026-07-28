using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.GetDetail;

public sealed class GetDsaProblemDetailHandler(
    IDsaProblemRepository repository,
    IDsaProblemAttemptDetailReader attemptDetailReader,
    ICurrentUser currentUser)
{
    private const int RecentAttemptCount = 5;

    public async Task<GetDsaProblemDetailResult> HandleAsync(
        GetDsaProblemDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await repository.GetByIdAndUserIdAsync(
            query.Id, userId, cancellationToken);
        if (problem is null)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        var attemptData = await attemptDetailReader.ReadAsync(
            problem.Id, RecentAttemptCount, cancellationToken);
        var averageDuration = attemptData.Summary.TotalAttempts == 0
            ? 0
            : Math.Round(
                attemptData.Summary.TotalDurationMinutes
                / (double)attemptData.Summary.TotalAttempts,
                2);

        return new GetDsaProblemDetailResult(
            problem.Id, problem.Title, problem.Description,
            problem.Difficulty.ToString(), problem.Source, problem.ExternalUrl,
            problem.Topics.OrderBy(topic => topic.NormalizedName)
                .Select(topic => topic.Name).ToList(),
            problem.Status.ToString(), problem.CreatedAtUtc,
            problem.UpdatedAtUtc,
            new DsaAttemptSummary(
                attemptData.Summary.TotalAttempts,
                attemptData.Summary.SolvedAttempts,
                attemptData.Summary.PartiallySolvedAttempts,
                attemptData.Summary.FailedAttempts,
                attemptData.Summary.SkippedAttempts,
                attemptData.Summary.TotalDurationMinutes,
                averageDuration,
                attemptData.Summary.LastAttemptedAtUtc),
            MapOptionalOverview(attemptData.LatestAttempt),
            MapOptionalOverview(attemptData.LatestSuccessfulAttempt),
            attemptData.RecentAttempts.Select(MapOverview).ToList());
    }

    private static DsaAttemptOverview MapOverview(
        Attempts.DsaAttemptOverviewReadItem attempt) =>
        new(
            attempt.Id, attempt.AttemptNumber, attempt.Result.ToString(),
            attempt.Language, attempt.TimeComplexity,
            attempt.SpaceComplexity, attempt.DurationMinutes,
            attempt.AttemptedAtUtc);

    private static DsaAttemptOverview? MapOptionalOverview(
        Attempts.DsaAttemptOverviewReadItem? attempt) =>
        attempt is null ? null : MapOverview(attempt);
}
