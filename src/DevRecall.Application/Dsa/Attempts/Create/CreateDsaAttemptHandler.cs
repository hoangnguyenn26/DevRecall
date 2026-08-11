using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.Create;

public sealed class CreateDsaAttemptHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser,
    IUtcClock clock)
{
    public async Task<CreateDsaAttemptResult> HandleAsync(
        CreateDsaAttemptCommand command,
        CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        if (command.SubmissionId == Guid.Empty)
            throw Validation("submissionId", "Submission ID cannot be empty.");
        if (command.StartedAtUtc is { Offset: var offset } && offset != TimeSpan.Zero)
            throw Validation("startedAtUtc", "Started time must be in UTC.");
        if (command.SubmissionId is Guid submissionId)
        {
            var existing = await attemptRepository.GetPracticeSubmissionAsync(userId, submissionId, cancellationToken);
            if (existing is not null)
            {
                if (existing.Value.Submission.DsaProblemId != command.DsaProblemId)
                    throw new ConflictException("DSA_PRACTICE_SUBMISSION_CONFLICT", "The submission ID belongs to another problem.");
                return Map(existing.Value.Attempt);
            }
        }

        var completedAtUtc = new DateTimeOffset(clock.UtcNow.Ticks / 10 * 10, TimeSpan.Zero);
        var durationMinutes = command.DurationMinutes;
        var attemptedAtUtc = command.AttemptedAtUtc;
        if (command.StartedAtUtc is DateTimeOffset startedAtUtc)
        {
            if (startedAtUtc > completedAtUtc || completedAtUtc - startedAtUtc > TimeSpan.FromDays(1))
                throw Validation("startedAtUtc", "Started time must be within the last 24 hours.");
            durationMinutes = Math.Max(0, (int)Math.Ceiling((completedAtUtc - startedAtUtc).TotalMinutes));
            attemptedAtUtc = completedAtUtc;
        }

        DsaAttemptInputValidator.Validate(
            command.Language, command.SolutionCode, command.Approach,
            command.TimeComplexity, command.SpaceComplexity,
            durationMinutes, command.Notes, attemptedAtUtc);
        var result = DsaAttemptResultParser.Parse(command.Result);
        var problem = await problemRepository.GetByIdAsync(
            command.DsaProblemId, cancellationToken);
        if (problem is null || problem.UserId != userId)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        if (problem.Status == DsaProblemStatus.Archived)
        {
            throw new ConflictException(
                DsaProblemErrors.Archived.Code,
                "An archived DSA problem cannot receive a new attempt.");
        }

        var attemptNumber =
            await attemptRepository.GetNextAttemptNumberAsync(
                problem.Id, cancellationToken);
        var attempt = DsaAttempt.Create(
            Guid.NewGuid(), problem.Id, attemptNumber, result,
            command.Language, command.SolutionCode, command.Approach,
            command.TimeComplexity, command.SpaceComplexity,
            durationMinutes, command.Notes, attemptedAtUtc, completedAtUtc);
        attemptRepository.Add(attempt);
        if (command.SubmissionId is Guid practiceSubmissionId && command.StartedAtUtc is DateTimeOffset practiceStartedAtUtc)
            attemptRepository.AddPracticeSubmission(DsaPracticeSubmission.Create(
                Guid.NewGuid(), userId, problem.Id, attempt.Id, practiceSubmissionId,
                problem.Title, problem.Difficulty.ToString(), practiceStartedAtUtc, completedAtUtc));
        await attemptRepository.SaveChangesAsync(cancellationToken);

        return Map(attempt);
    }

    private static CreateDsaAttemptResult Map(DsaAttempt attempt) =>
        new(
            attempt.Id, attempt.DsaProblemId, attempt.AttemptNumber,
            attempt.Result.ToString(), attempt.Language, attempt.SolutionCode,
            attempt.Approach, attempt.TimeComplexity,
            attempt.SpaceComplexity, attempt.DurationMinutes, attempt.Notes,
            attempt.AttemptedAtUtc, attempt.CreatedAtUtc);

    private static ValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
