using DevRecall.Application.Search;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Search;

internal sealed class GlobalSearchReader(DevRecallDbContext dbContext)
    : IGlobalSearchReader
{
    private const int SummaryLength = 240;

    public async Task<GlobalSearchReadModel> SearchAsync(
        Guid userId, string query, int takePerType,
        CancellationToken cancellationToken)
    {
        var limit = takePerType + 1;
        var knowledge = await SearchKnowledgeAsync(
            userId, query, limit, cancellationToken);
        var interview = await SearchInterviewAsync(
            userId, query, limit, cancellationToken);
        var dsa = await SearchDsaAsync(
            userId, query, limit, cancellationToken);
        var hasMore = knowledge.Count > takePerType
            || interview.Count > takePerType || dsa.Count > takePerType;

        var items = knowledge.Take(takePerType)
            .Concat(interview.Take(takePerType))
            .Concat(dsa.Take(takePerType))
            .OrderByDescending(item => item.IsExactTitleMatch)
            .ThenByDescending(item => item.IsPrefixTitleMatch)
            .ThenByDescending(item => item.Rank)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.ResourceType)
            .ThenBy(item => item.ResourceId)
            .ToList();
        return new GlobalSearchReadModel(items, hasMore);
    }

    private async Task<IReadOnlyList<GlobalSearchCandidate>> SearchKnowledgeAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var exactPattern = EscapeLikePattern(text);
        var prefixPattern = exactPattern + "%";
        var rows = await dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId
                && node.Status == KnowledgeNodeStatus.Active)
            .Select(node => new
            {
                Node = node,
                Vector = EF.Functions.ToTsVector("simple",
                    node.Title + " " + node.Content + " " + (node.Description ?? "")),
                IsExact = EF.Functions.ILike(node.Title, exactPattern, "\\"),
                IsPrefix = EF.Functions.ILike(node.Title, prefixPattern, "\\")
            })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)))
            .OrderByDescending(row => row.IsExact)
            .ThenByDescending(row => row.IsPrefix)
            .ThenByDescending(row => row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text)))
            .ThenByDescending(row => row.Node.UpdatedAtUtc)
            .ThenBy(row => row.Node.Id)
            .Take(limit)
            .Select(row => new
            {
                row.Node.Id,
                row.Node.Title,
                Summary = row.Node.Description ?? (row.Node.Content.Length > SummaryLength
                    ? row.Node.Content.Substring(0, SummaryLength) : row.Node.Content),
                row.Node.UpdatedAtUtc,
                row.IsExact,
                row.IsPrefix,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new GlobalSearchCandidate(
            row.Id, GlobalSearchResourceType.Knowledge, row.Title,
            NormalizeSummary(row.Summary), Convert.ToDecimal(row.Rank),
            row.UpdatedAtUtc, row.IsExact, row.IsPrefix)).ToList();
    }

    private async Task<IReadOnlyList<GlobalSearchCandidate>> SearchInterviewAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var exactPattern = EscapeLikePattern(text);
        var prefixPattern = exactPattern + "%";
        var rows = await dbContext.InterviewQuestions.AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == InterviewQuestionStatus.Active)
            .Select(item => new
            {
                Item = item,
                Vector = EF.Functions.ToTsVector("simple",
                    item.Title + " " + item.Question + " " + item.Topic + " " + (item.Notes ?? "")),
                IsExact = EF.Functions.ILike(item.Title, exactPattern, "\\"),
                IsPrefix = EF.Functions.ILike(item.Title, prefixPattern, "\\")
            })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)))
            .OrderByDescending(row => row.IsExact)
            .ThenByDescending(row => row.IsPrefix)
            .ThenByDescending(row => row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text)))
            .ThenByDescending(row => row.Item.UpdatedAtUtc)
            .ThenBy(row => row.Item.Id)
            .Take(limit)
            .Select(row => new
            {
                row.Item.Id,
                row.Item.Title,
                Summary = row.Item.Question.Length > SummaryLength
                    ? row.Item.Question.Substring(0, SummaryLength) : row.Item.Question,
                row.Item.UpdatedAtUtc,
                row.IsExact,
                row.IsPrefix,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new GlobalSearchCandidate(
            row.Id, GlobalSearchResourceType.InterviewQuestion, row.Title,
            NormalizeSummary(row.Summary), Convert.ToDecimal(row.Rank),
            row.UpdatedAtUtc, row.IsExact, row.IsPrefix)).ToList();
    }

    private async Task<IReadOnlyList<GlobalSearchCandidate>> SearchDsaAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var exactPattern = EscapeLikePattern(text);
        var prefixPattern = exactPattern + "%";
        var rows = await dbContext.DsaProblems.AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == DsaProblemStatus.Active)
            .Select(item => new
            {
                Item = item,
                Vector = EF.Functions.ToTsVector("simple",
                    item.Title + " " + item.Description + " " + (item.Source ?? "")),
                IsExact = EF.Functions.ILike(item.Title, exactPattern, "\\"),
                IsPrefix = EF.Functions.ILike(item.Title, prefixPattern, "\\")
            })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)))
            .OrderByDescending(row => row.IsExact)
            .ThenByDescending(row => row.IsPrefix)
            .ThenByDescending(row => row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text)))
            .ThenByDescending(row => row.Item.UpdatedAtUtc)
            .ThenBy(row => row.Item.Id)
            .Take(limit)
            .Select(row => new
            {
                row.Item.Id,
                row.Item.Title,
                Summary = row.Item.Description.Length > SummaryLength
                    ? row.Item.Description.Substring(0, SummaryLength) : row.Item.Description,
                row.Item.UpdatedAtUtc,
                row.IsExact,
                row.IsPrefix,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new GlobalSearchCandidate(
            row.Id, GlobalSearchResourceType.DsaProblem, row.Title,
            NormalizeSummary(row.Summary), Convert.ToDecimal(row.Rank),
            row.UpdatedAtUtc, row.IsExact, row.IsPrefix)).ToList();
    }

    private static string? NormalizeSummary(string? summary)
    {
        var normalized = summary?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
