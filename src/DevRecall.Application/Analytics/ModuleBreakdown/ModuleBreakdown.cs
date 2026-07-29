using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Analytics.ModuleBreakdown;

public sealed record GetModuleBreakdownQuery(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
public sealed record ModuleActivityCount(
    StudyResourceType ResourceType, int CompletedItems);
public sealed record ModuleBreakdownItem(
    string ResourceType, int CompletedItems, decimal Percentage);
public sealed record GetModuleBreakdownResult(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalCompletedItems,
    IReadOnlyList<ModuleBreakdownItem> Modules);

public interface IModuleBreakdownReader
{
    Task<IReadOnlyList<ModuleActivityCount>> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken);
}

public sealed class GetModuleBreakdownHandler(
    AnalyticsDateRangeResolver dateRangeResolver,
    IModuleBreakdownReader reader,
    ICurrentUser currentUser)
{
    private static readonly StudyResourceType[] SupportedResourceTypes =
    [
        StudyResourceType.KnowledgeNode,
        StudyResourceType.InterviewQuestion,
        StudyResourceType.DsaProblem,
        StudyResourceType.ReviewItem
    ];

    public async Task<GetModuleBreakdownResult> HandleAsync(
        GetModuleBreakdownQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var range = dateRangeResolver.Resolve(
            new AnalyticsDateRangeInput(query.FromUtc, query.ToUtc));
        var counts = await reader.ReadAsync(
            userId, range, cancellationToken);
        var countByType = counts.ToDictionary(
            item => item.ResourceType, item => item.CompletedItems);
        var total = countByType.Values.Sum();
        var modules = SupportedResourceTypes.Select(resourceType =>
        {
            var completedItems = countByType.GetValueOrDefault(resourceType);
            return new ModuleBreakdownItem(
                resourceType.ToString(), completedItems,
                CalculatePercentage(completedItems, total));
        }).ToList();
        return new GetModuleBreakdownResult(
            range.FromUtc, range.ToUtc, total, modules);
    }

    private static decimal CalculatePercentage(
        int completedItems, int totalCompletedItems) =>
        totalCompletedItems == 0
            ? 0m
            : Math.Round(
                completedItems / (decimal)totalCompletedItems * 100m,
                2, MidpointRounding.AwayFromZero);

    private Guid GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
