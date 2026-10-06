using DevRecall.Application.Discover;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Discover;

internal sealed class DiscoverReader(DevRecallDbContext dbContext) : IDiscoverReader
{
    public async Task<DiscoverInputs> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.LearningProfiles.AsNoTracking().AsSplitQuery().Include(item => item.Goals)
            .Include(item => item.Technologies).SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var signals = profile is null
            ? new LearningProfileSignals(null, null, new HashSet<Technology>(),
                new HashSet<Technology>(), new HashSet<LearningProfileGoal>(), null, false)
            : new LearningProfileSignals(profile.TargetRole, profile.ExperienceLevel,
                profile.Technologies.Where(item => item.IsPrimary).Select(item => item.Technology).ToHashSet(),
                profile.Technologies.Where(item => !item.IsPrimary).Select(item => item.Technology).ToHashSet(),
                profile.Goals.Select(item => item.Goal).ToHashSet(), profile.AvailableMinutesPerDay, profile.IsConfigured);
        if (profile is null) return new(signals, [], []);
        var goals = signals.Goals.ToArray();
        var technologies = signals.PrimaryTechnologies.Concat(signals.OtherTechnologies).ToArray();
        var lessons = await dbContext.LearningContents.AsNoTracking().AsSplitQuery()
            .Where(item => item.Status == ContentStatus.Published && item.ContentType == LearningContentType.Lesson
                && (item.Goals.Any(goal => goals.Contains(goal.Goal))
                    || item.Technologies.Any(value => technologies.Contains(value.Technology)))
                && !dbContext.LearningContentProgresses.Any(progress => progress.UserId == userId
                    && progress.LearningContentId == item.Id)
                && !dbContext.LearningContentCompletionEvidence.Any(evidence => evidence.UserId == userId
                    && evidence.LearningContentId == item.Id))
            .OrderByDescending(item => item.PublishedAtUtc).ThenBy(item => item.Id).Take(LearningRecommendationPolicy.CandidateLimit)
            .Select(item => new
            {
                item.Id, item.Slug, item.Title, item.Summary, item.Difficulty, item.EstimatedMinutes, item.PublishedAtUtc,
                Goals = item.Goals.Select(goal => goal.Goal).ToArray(),
                Technologies = item.Technologies.Select(value => value.Technology).ToArray(),
                Topics = item.Topics.Select(link => new DiscoverTopic(link.TopicId,
                    dbContext.ContentTopics.Where(topic => topic.Id == link.TopicId).Select(topic => topic.Slug).First(),
                    dbContext.ContentTopics.Where(topic => topic.Id == link.TopicId).Select(topic => topic.Name).First())).ToArray()
            }).ToListAsync(cancellationToken);
        // No trustworthy global Content Topic ↔ user Weak Topic mapping exists yet.
        return new(signals, [], lessons.Select(item => new DiscoverCandidate(item.Id, item.Slug, item.Title, item.Summary,
            item.Difficulty, item.EstimatedMinutes, item.PublishedAtUtc!.Value, item.Technologies,
            item.Goals, item.Topics)).ToArray());
    }
}
