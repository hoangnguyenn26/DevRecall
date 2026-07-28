using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.Compare;

public sealed class CompareDsaAttemptsHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser)
{
    public async Task<CompareDsaAttemptsResult> HandleAsync(
        CompareDsaAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await problemRepository.GetByIdAndUserIdAsync(
            query.DsaProblemId, userId, cancellationToken);
        if (problem is null)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        var attempts = await attemptRepository.GetByIdsAndProblemIdAsync(
            problem.Id, [query.LeftAttemptId, query.RightAttemptId],
            cancellationToken);
        var left = attempts.SingleOrDefault(
            attempt => attempt.Id == query.LeftAttemptId);
        var right = attempts.SingleOrDefault(
            attempt => attempt.Id == query.RightAttemptId);
        if (left is null || right is null)
        {
            throw new NotFoundException(
                DsaAttemptErrors.NotFound.Code,
                DsaAttemptErrors.NotFound.Message);
        }

        return new CompareDsaAttemptsResult(
            problem.Id, MapSnapshot(left), MapSnapshot(right),
            BuildDifference(left, right));
    }

    private static void ValidateQuery(CompareDsaAttemptsQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.LeftAttemptId == Guid.Empty)
        {
            errors["leftAttemptId"] = ["Left attempt id is required."];
        }

        if (query.RightAttemptId == Guid.Empty)
        {
            errors["rightAttemptId"] = ["Right attempt id is required."];
        }

        if (query.LeftAttemptId != Guid.Empty
            && query.LeftAttemptId == query.RightAttemptId)
        {
            errors["rightAttemptId"] =
                ["Right attempt must be different from left attempt."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static DsaAttemptComparisonSnapshot MapSnapshot(
        DsaAttempt attempt) =>
        new(
            attempt.Id, attempt.AttemptNumber, attempt.Result.ToString(),
            attempt.Language, attempt.SolutionCode, attempt.Approach,
            attempt.TimeComplexity, attempt.SpaceComplexity,
            attempt.DurationMinutes, attempt.Notes, attempt.AttemptedAtUtc,
            attempt.CreatedAtUtc);

    private static DsaAttemptComparisonDifference BuildDifference(
        DsaAttempt left, DsaAttempt right) =>
        new(
            right.AttemptNumber - left.AttemptNumber,
            right.DurationMinutes - left.DurationMinutes,
            $"{left.Result} -> {right.Result}",
            left.Result != right.Result,
            !EquivalentSingleLine(left.Language, right.Language),
            !EquivalentMultiline(left.SolutionCode, right.SolutionCode),
            !EquivalentMultiline(left.Approach, right.Approach),
            !EquivalentSingleLine(
                left.TimeComplexity, right.TimeComplexity),
            !EquivalentSingleLine(
                left.SpaceComplexity, right.SpaceComplexity),
            !EquivalentMultiline(left.Notes, right.Notes));

    private static bool EquivalentSingleLine(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool EquivalentMultiline(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}
