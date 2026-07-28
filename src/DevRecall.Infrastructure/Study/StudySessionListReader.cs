using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Study.GetList;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudySessionListReader(DevRecallDbContext dbContext)
    : IStudySessionListReader
{
    public async Task<PagedReadResult<StudySessionListReadModel>> ReadAsync(
        Guid userId, StudySessionStatus? status, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId);
        if (status is not null)
        {
            query = query.Where(session => session.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(session => session.CreatedAtUtc)
            .ThenByDescending(session => session.Id)
            .Skip(skip)
            .Take(take)
            .Select(session => new StudySessionListReadModel(
                session.Id, session.Title, session.Status,
                session.PlannedDurationMinutes,
                session.ActualDurationMinutes, session.StartedAtUtc,
                session.CompletedAtUtc, session.Items.Count,
                session.Items.Count(item =>
                    item.Status == StudySessionItemStatus.Completed),
                session.Items.Count(item =>
                    item.Status == StudySessionItemStatus.Skipped),
                session.Version, session.CreatedAtUtc,
                session.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedReadResult<StudySessionListReadModel>(
            items, totalCount);
    }
}
