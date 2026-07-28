using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.GetDetail;

internal static class DsaAttemptDetailMapper
{
    public static GetDsaAttemptDetailResult Map(DsaAttempt attempt) =>
        new(
            attempt.Id, attempt.DsaProblemId, attempt.AttemptNumber,
            attempt.Result.ToString(), attempt.Language, attempt.SolutionCode,
            attempt.Approach, attempt.TimeComplexity,
            attempt.SpaceComplexity, attempt.DurationMinutes, attempt.Notes,
            attempt.AttemptedAtUtc, attempt.CreatedAtUtc);
}
