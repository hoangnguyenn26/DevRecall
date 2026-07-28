using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.GetList;

public sealed class GetDsaAttemptsHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser)
{
    private const int MaximumPageSize = 100;

    public async Task<GetDsaAttemptsResult> HandleAsync(
        GetDsaAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await problemRepository.GetByIdAndUserIdAsync(
            query.DsaProblemId, userId, cancellationToken);
        if (problem is null)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        DsaAttemptResult? resultFilter = string.IsNullOrWhiteSpace(query.Result)
            ? null
            : DsaAttemptResultParser.Parse(query.Result);
        var attempts = await attemptRepository.GetListAsync(
            problem.Id, resultFilter, (query.Page - 1) * query.PageSize,
            query.PageSize, cancellationToken);
        var totalPages = attempts.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                attempts.TotalCount / (double)query.PageSize);

        return new GetDsaAttemptsResult(
            attempts.Items.Select(attempt => new DsaAttemptListItem(
                attempt.Id, attempt.AttemptNumber, attempt.Result.ToString(),
                attempt.Language, attempt.TimeComplexity,
                attempt.SpaceComplexity, attempt.DurationMinutes,
                attempt.AttemptedAtUtc, attempt.CreatedAtUtc)).ToList(),
            query.Page, query.PageSize, attempts.TotalCount, totalPages);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            errors["pageSize"] =
                [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
