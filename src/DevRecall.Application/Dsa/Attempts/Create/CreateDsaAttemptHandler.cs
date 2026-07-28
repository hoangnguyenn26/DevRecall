using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.Create;

public sealed class CreateDsaAttemptHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser)
{
    public async Task<CreateDsaAttemptResult> HandleAsync(
        CreateDsaAttemptCommand command,
        CancellationToken cancellationToken)
    {
        DsaAttemptInputValidator.Validate(
            command.Language, command.SolutionCode, command.Approach,
            command.TimeComplexity, command.SpaceComplexity,
            command.DurationMinutes, command.Notes, command.AttemptedAtUtc);
        var result = DsaAttemptResultParser.Parse(command.Result);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
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
            command.DurationMinutes, command.Notes, command.AttemptedAtUtc,
            DateTimeOffset.UtcNow);
        attemptRepository.Add(attempt);
        await attemptRepository.SaveChangesAsync(cancellationToken);

        return new CreateDsaAttemptResult(
            attempt.Id, attempt.DsaProblemId, attempt.AttemptNumber,
            attempt.Result.ToString(), attempt.Language, attempt.SolutionCode,
            attempt.Approach, attempt.TimeComplexity,
            attempt.SpaceComplexity, attempt.DurationMinutes, attempt.Notes,
            attempt.AttemptedAtUtc, attempt.CreatedAtUtc);
    }
}
