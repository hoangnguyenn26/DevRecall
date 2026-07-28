using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.GetList;

public sealed record GetStudySessionsQuery(
    string? Status, int Page, int PageSize);

public sealed record StudySessionListReadModel(
    Guid Id, string Title, StudySessionStatus Status,
    int PlannedDurationMinutes, int? ActualDurationMinutes,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int TotalItems, int CompletedItems, int SkippedItems,
    int Version, DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionListItem(
    Guid Id, string Title, string Status,
    int PlannedDurationMinutes, int? ActualDurationMinutes,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int TotalItems, int CompletedItems, int SkippedItems,
    int Version, DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record GetStudySessionsResult(
    IReadOnlyList<StudySessionListItem> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);

public interface IStudySessionListReader
{
    Task<PagedReadResult<StudySessionListReadModel>> ReadAsync(
        Guid userId, StudySessionStatus? status, int skip, int take,
        CancellationToken cancellationToken);
}

public sealed class GetStudySessionsHandler(
    IStudySessionListReader listReader,
    ICurrentUser currentUser)
{
    public async Task<GetStudySessionsResult> HandleAsync(
        GetStudySessionsQuery query,
        CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var userId = StudySessionSupport.GetUserId(currentUser);
        StudySessionStatus? status = string.IsNullOrWhiteSpace(query.Status)
            ? null
            : StudySessionStatusParser.Parse(query.Status);
        var page = await listReader.ReadAsync(
            userId, status, (query.Page - 1) * query.PageSize,
            query.PageSize, cancellationToken);
        var totalPages = page.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(
                page.TotalCount / (double)query.PageSize);
        return new GetStudySessionsResult(
            page.Items.Select(item => new StudySessionListItem(
                item.Id, item.Title, item.Status.ToString(),
                item.PlannedDurationMinutes, item.ActualDurationMinutes,
                item.StartedAtUtc, item.CompletedAtUtc, item.TotalItems,
                item.CompletedItems, item.SkippedItems, item.Version,
                item.CreatedAtUtc, item.UpdatedAtUtc)).ToList(),
            query.Page, query.PageSize, page.TotalCount, totalPages);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
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
