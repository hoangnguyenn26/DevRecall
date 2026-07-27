using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.GetList;

public sealed class GetDsaProblemsHandler(
    IDsaProblemRepository repository,
    ICurrentUser currentUser)
{
    private const int MaximumPageSize = 100;

    public async Task<GetDsaProblemsResult> HandleAsync(
        GetDsaProblemsQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var difficulty =
            DsaProblemDifficultyParser.ParseOptional(query.Difficulty);
        var normalizedTopic = NormalizeTopic(query.Topic);
        var normalizedSource = NormalizeSource(query.Source);
        var skip = (query.Page - 1) * query.PageSize;
        var result = await repository.GetActiveListAsync(
            userId, difficulty, normalizedTopic, normalizedSource, skip,
            query.PageSize, cancellationToken);
        var topicRows = await repository.GetTopicsByProblemIdsAsync(
            result.Items.Select(item => item.Id).ToArray(),
            cancellationToken);
        var topicsByProblemId = topicRows
            .GroupBy(row => row.DsaProblemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .OrderBy(row => row.Name)
                    .Select(row => row.Name)
                    .ToList());
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                result.TotalCount / (double)query.PageSize);

        return new GetDsaProblemsResult(
            result.Items.Select(problem => new DsaProblemListItem(
                problem.Id, problem.Title, problem.Difficulty.ToString(),
                problem.Source,
                topicsByProblemId.TryGetValue(problem.Id, out var topics)
                    ? topics
                    : [],
                problem.UpdatedAtUtc)).ToList(),
            query.Page, query.PageSize, result.TotalCount, totalPages);
    }

    private static void ValidatePagination(GetDsaProblemsQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        if (query.Page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (query.PageSize < 1 || query.PageSize > MaximumPageSize)
        {
            errors["pageSize"] =
                [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static string? NormalizeTopic(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return null;
        }

        var normalized = DsaProblemInputValidator.NormalizeSingleLine(topic);
        if (normalized.Length > DsaProblemTopic.MaxLength)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["topic"] =
                    [
                        $"Topic cannot exceed {DsaProblemTopic.MaxLength} characters."
                    ]
                });
        }

        return normalized.ToUpperInvariant();
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var normalized = DsaProblemInputValidator.NormalizeSingleLine(source);
        if (normalized.Length > DsaProblemText.SourceMaxLength)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["source"] =
                    [
                        $"Source cannot exceed {DsaProblemText.SourceMaxLength} characters."
                    ]
                });
        }

        return normalized;
    }
}
