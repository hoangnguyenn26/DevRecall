using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.GetList;

public sealed record GetStudyPlansQuery(
    string? Status, int Page, int PageSize);

public sealed record StudyPlanListReadModel(
    Guid StudyPlanId, string Title, StudyPlanStatus Status,
    int ItemCount, int TotalPlannedDurationMinutes,
    DateTimeOffset GeneratedAtUtc, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId, DateTimeOffset? CancelledAtUtc,
    DateTimeOffset UpdatedAtUtc, int Version);

public sealed record StudyPlanListItem(
    Guid StudyPlanId, string Title, string Status,
    int ItemCount, int TotalPlannedDurationMinutes,
    DateTimeOffset GeneratedAtUtc, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId, DateTimeOffset? CancelledAtUtc,
    DateTimeOffset UpdatedAtUtc, int Version);

public sealed record GetStudyPlansResult(
    IReadOnlyList<StudyPlanListItem> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);

public interface IStudyPlanListReader
{
    Task<PagedReadResult<StudyPlanListReadModel>> ReadAsync(
        Guid userId, StudyPlanStatus? status, int skip, int take,
        CancellationToken cancellationToken);
}

public sealed class GetStudyPlansHandler(
    IStudyPlanListReader reader, ICurrentUser currentUser)
{
    public async Task<GetStudyPlansResult> HandleAsync(
        GetStudyPlansQuery query, CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "AUTH_REQUIRED", "Authentication is required.");
        StudyPlanStatus? status = string.IsNullOrWhiteSpace(query.Status)
            ? null : StudyPlanStatusParser.Parse(query.Status);
        var skip = checked((query.Page - 1) * query.PageSize);
        var page = await reader.ReadAsync(
            userId, status, skip, query.PageSize, cancellationToken);
        var items = page.Items.Select(item => new StudyPlanListItem(
            item.StudyPlanId, item.Title, item.Status.ToString(),
            item.ItemCount, item.TotalPlannedDurationMinutes,
            item.GeneratedAtUtc, item.ExpiresAtUtc, item.ReadyAtUtc,
            item.ConvertedAtUtc, item.ConvertedStudySessionId,
            item.CancelledAtUtc, item.UpdatedAtUtc, item.Version)).ToArray();
        var totalPages = page.TotalCount == 0
            ? 0 : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize);
        return new(
            items, query.Page, query.PageSize, page.TotalCount, totalPages);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Page must be at least 1."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

public static class StudyPlanStatusParser
{
    public static StudyPlanStatus Parse(string value) =>
        Normalize(value) switch
        {
            "draft" => StudyPlanStatus.Draft,
            "ready" => StudyPlanStatus.Ready,
            "converted" => StudyPlanStatus.Converted,
            "cancelled" => StudyPlanStatus.Cancelled,
            _ => throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = ["Status is invalid."]
                })
        };

    private static string Normalize(string value) =>
        value.Trim().Replace(" ", string.Empty)
            .Replace("-", string.Empty).Replace("_", string.Empty)
            .ToLowerInvariant();
}
