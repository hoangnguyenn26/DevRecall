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
    public async Task<GlobalSearchReadResult> SearchAsync(
        Guid userId, string text, IReadOnlySet<string> modules,
        int candidateLimit, CancellationToken cancellationToken)
    {
        var candidates = new List<GlobalSearchCandidate>();
        var totalCount = 0;
        if (modules.Contains("knowledge"))
        {
            var result = await SearchKnowledgeAsync(userId, text, candidateLimit, cancellationToken);
            candidates.AddRange(result.Items); totalCount += result.TotalCount;
        }
        if (modules.Contains("interview"))
        {
            var result = await SearchInterviewAsync(userId, text, candidateLimit, cancellationToken);
            candidates.AddRange(result.Items); totalCount += result.TotalCount;
        }
        if (modules.Contains("dsa"))
        {
            var result = await SearchDsaAsync(userId, text, candidateLimit, cancellationToken);
            candidates.AddRange(result.Items); totalCount += result.TotalCount;
        }
        return new GlobalSearchReadResult(
            candidates.OrderByDescending(item => item.Rank)
                .ThenBy(item => item.Title).Take(candidateLimit).ToList(), totalCount);
    }

    private async Task<GlobalSearchReadResult> SearchKnowledgeAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var rows = dbContext.KnowledgeNodes.AsNoTracking()
            .Where(node => node.UserId == userId && node.Status == KnowledgeNodeStatus.Active)
            .Select(node => new { Node = node,
                Vector = EF.Functions.ToTsVector("simple", node.Title + " " + node.Content + " " + (node.Description ?? "")) })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)));
        var total = await rows.CountAsync(cancellationToken);
        var matches = await rows.OrderByDescending(row => row.Vector.RankCoverDensity(
                EF.Functions.WebSearchToTsQuery("simple", text)))
            .Take(limit).Select(row => new
            {
                row.Node.Id, row.Node.Title, row.Node.Content, row.Node.Description,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            }).ToListAsync(cancellationToken);
        var items = matches.Select(row => new GlobalSearchCandidate(
            "Knowledge", row.Id, row.Title, row.Description ?? row.Content,
            row.Rank, new Dictionary<string, string>())).ToList();
        return new GlobalSearchReadResult(items.Select(TrimPreview).ToList(), total);
    }

    private async Task<GlobalSearchReadResult> SearchInterviewAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var rows = dbContext.InterviewQuestions.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == InterviewQuestionStatus.Active)
            .Select(item => new { Item = item,
                Vector = EF.Functions.ToTsVector("simple", item.Title + " " + item.Question + " " + item.Topic + " " + (item.Notes ?? "")) })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)));
        var total = await rows.CountAsync(cancellationToken);
        var matches = await rows.OrderByDescending(
                row => row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text)))
            .Take(limit).Select(row => new
            {
                row.Item.Id, row.Item.Title, row.Item.Question,
                row.Item.Topic, row.Item.Difficulty,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            }).ToListAsync(cancellationToken);
        var items = matches.Select(row => new GlobalSearchCandidate(
            "Interview", row.Id, row.Title, row.Question, row.Rank,
            new Dictionary<string, string>
            {
                ["topic"] = row.Topic,
                ["difficulty"] = row.Difficulty.ToString()
            })).ToList();
        return new GlobalSearchReadResult(items.Select(TrimPreview).ToList(), total);
    }

    private async Task<GlobalSearchReadResult> SearchDsaAsync(
        Guid userId, string text, int limit, CancellationToken cancellationToken)
    {
        var rows = dbContext.DsaProblems.AsNoTracking()
            .Where(item => item.UserId == userId && item.Status == DsaProblemStatus.Active)
            .Select(item => new { Item = item,
                Vector = EF.Functions.ToTsVector("simple", item.Title + " " + item.Description + " " + (item.Source ?? "")) })
            .Where(row => row.Vector.Matches(EF.Functions.WebSearchToTsQuery("simple", text)));
        var total = await rows.CountAsync(cancellationToken);
        var matches = await rows.OrderByDescending(
                row => row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text)))
            .Take(limit).Select(row => new
            {
                row.Item.Id, row.Item.Title, row.Item.Description,
                row.Item.Difficulty, row.Item.Source,
                Rank = row.Vector.RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", text))
            }).ToListAsync(cancellationToken);
        var items = matches.Select(row => new GlobalSearchCandidate(
            "Dsa", row.Id, row.Title, row.Description, row.Rank,
            new Dictionary<string, string>
            {
                ["difficulty"] = row.Difficulty.ToString(),
                ["source"] = row.Source ?? ""
            })).ToList();
        return new GlobalSearchReadResult(items.Select(TrimPreview).ToList(), total);
    }

    private static GlobalSearchCandidate TrimPreview(GlobalSearchCandidate item) =>
        item with { Preview = item.Preview is { Length: > 240 } preview
            ? $"{preview[..240]}..." : item.Preview };
}
