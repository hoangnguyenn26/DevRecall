using DevRecall.Application.LearningContent;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.LearningContent;

internal sealed class LearningContentReader(DevRecallDbContext dbContext) : ILearningContentReader
{
    public async Task<PublishedLearningContentPage> GetPublishedAsync(Guid userId, string? technology,
        string? topicSlug, string? difficulty, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.LearningContents.AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published);
        if (technology is not null)
        {
            var parsed = Enum.Parse<Technology>(technology);
            query = query.Where(item => item.Technologies.Any(value => value.Technology == parsed));
        }
        if (difficulty is not null)
        {
            var parsed = Enum.Parse<ContentDifficulty>(difficulty);
            query = query.Where(item => item.Difficulty == parsed);
        }
        if (topicSlug is not null)
        {
            query = query.Where(item => item.Topics.Any(link => dbContext.ContentTopics
                .Any(topic => topic.Id == link.TopicId && topic.Slug == topicSlug)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rawItems = await query.OrderByDescending(item => item.PublishedAtUtc)
            .ThenBy(item => item.Id).Skip(skip).Take(take)
            .Select(item => new
            {
                item.Id,
                item.Slug,
                item.Title,
                item.Summary,
                item.ContentType,
                item.Difficulty,
                item.EstimatedMinutes,
                ProgressStatus = dbContext.LearningContentProgresses.Where(progress =>
                    progress.UserId == userId && progress.LearningContentId == item.Id)
                    .Select(progress => (LearningProgressStatus?)progress.Status).SingleOrDefault(),
                Technologies = item.Technologies.OrderBy(value => value.Technology)
                    .Select(value => value.Technology).ToArray(),
                TopicIds = item.Topics.Select(value => value.TopicId).ToArray()
            }).ToListAsync(cancellationToken);
        var topicIds = rawItems.SelectMany(item => item.TopicIds).Distinct().ToArray();
        var topics = await dbContext.ContentTopics.AsNoTracking().Where(item => topicIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var items = rawItems.Select(item => new PublishedLearningContentListItem(
            item.Slug, item.Title, item.Summary, item.ContentType.ToString(), item.Difficulty.ToString(),
            item.EstimatedMinutes, item.Technologies.Select(MapTechnology).ToArray(),
            item.TopicIds.Select(id => new LearningContentTopicItem(topics[id].Slug, topics[id].Name))
                .OrderBy(topic => topic.Name).ToArray(), item.ProgressStatus?.ToString() ?? "NotStarted")).ToArray();
        return new(items, totalCount);
    }

    public async Task<PublishedLearningContentDetail?> GetPublishedBySlugAsync(Guid userId, string slug,
        CancellationToken cancellationToken)
    {
        var raw = await dbContext.LearningContents.AsNoTracking()
            .Where(item => item.Status == ContentStatus.Published && item.Slug == slug)
            .Select(item => new
            {
                item.Slug,
                item.Title,
                item.Summary,
                item.ContentType,
                item.Difficulty,
                item.EstimatedMinutes,
                item.SourceType,
                item.SourceName,
                item.SourceUrl,
                PublishedAtUtc = item.PublishedAtUtc!.Value,
                Progress = dbContext.LearningContentProgresses.Where(progress =>
                    progress.UserId == userId && progress.LearningContentId == item.Id)
                    .Select(progress => new { progress.Status, progress.StartedAtUtc,
                        progress.CompletedAtUtc, progress.Version }).SingleOrDefault(),
                Technologies = item.Technologies.OrderBy(value => value.Technology)
                    .Select(value => value.Technology).ToArray(),
                TopicIds = item.Topics.Select(value => value.TopicId).ToArray(),
                Objectives = item.Objectives.OrderBy(value => value.Position)
                    .Select(value => new { value.Position, value.Text }).ToArray(),
                Sections = item.Sections.OrderBy(value => value.Position)
                    .Select(value => new { value.Position, value.SectionType, value.Heading, value.BodyMarkdown }).ToArray()
            }).SingleOrDefaultAsync(cancellationToken);
        if (raw is null) return null;
        var topics = await dbContext.ContentTopics.AsNoTracking().Where(item => raw.TopicIds.Contains(item.Id))
            .OrderBy(item => item.Name).Select(item => new LearningContentTopicItem(item.Slug, item.Name))
            .ToArrayAsync(cancellationToken);
        return new(raw.Slug, raw.Title, raw.Summary, raw.ContentType.ToString(), raw.Difficulty.ToString(),
            raw.EstimatedMinutes, raw.Technologies.Select(MapTechnology).ToArray(), topics,
            raw.Objectives.Select(item => new LearningContentObjectiveItem(item.Position, item.Text)).ToArray(),
            raw.Sections.Select(item => new LearningContentSectionItem(item.Position,
                item.SectionType.ToString(), item.Heading, item.BodyMarkdown)).ToArray(),
            new(raw.SourceType.ToString(), raw.SourceName, raw.SourceUrl), raw.PublishedAtUtc,
            raw.Progress is null ? new("NotStarted", null, null, null) : new(raw.Progress.Status.ToString(),
                raw.Progress.StartedAtUtc, raw.Progress.CompletedAtUtc, raw.Progress.Version));
    }

    private static LearningContentTechnologyItem MapTechnology(Technology value)
    {
        var metadata = LearningProfileMetadata.TechnologyValue(value, false);
        return new(metadata.Value, metadata.Label);
    }
}
