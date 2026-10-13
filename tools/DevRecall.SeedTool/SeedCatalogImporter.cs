using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Content = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.SeedTool;

internal sealed class SeedCatalogImporter(DevRecallDbContext dbContext)
{
    private static readonly HashSet<string> KnownUniqueConstraints =
    [
        "uq_learning_contents_slug",
        "uq_content_topics_slug",
        "ux_learning_review_candidates_content_key",
        "ux_learning_review_candidates_content_position"
    ];

    public async Task<SeedImportResult> ApplyAsync(SeedCatalog catalog, SeedImportPlan plan,
        string? requestedSlug, CancellationToken cancellationToken)
    {
        var selected = plan.Items.Where(item => requestedSlug is null || item.Slug == requestedSlug).ToArray();
        var sourceBySlug = catalog.Items.ToDictionary(item => item.Slug!, StringComparer.Ordinal);
        var selectedTopicSlugs = selected.SelectMany(item => sourceBySlug[item.Slug].Topics!)
            .ToHashSet(StringComparer.Ordinal);
        var selectedTopics = plan.Topics.Where(item => selectedTopicSlugs.Contains(item.Slug)).ToArray();
        await SynchronizeTopicsAsync(selectedTopics, cancellationToken);
        var applied = new List<SeedImportPlanItem>();
        var failures = new List<SeedImportFailure>();

        foreach (var item in selected)
        {
            if (item.Action == SeedPlanAction.Unchanged)
            {
                applied.Add(item);
                continue;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var source = sourceBySlug[item.Slug];
                if (item.Action == SeedPlanAction.Create)
                    Create(source, catalog.Topics);
                else
                    await UpdateAsync(source, catalog.Topics, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                applied.Add(item);
            }
            catch (Exception exception) when (exception is DbUpdateException
                                               or DbUpdateConcurrencyException
                                               or InvalidOperationException
                                               or ArgumentException)
            {
                await transaction.RollbackAsync(cancellationToken);
                failures.Add(new(item.Slug, SafeFailureMessage(exception)));
            }
            finally
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return new(selectedTopics, applied, failures);
    }

    private async Task SynchronizeTopicsAsync(IReadOnlyList<SeedTopicPlanItem> topics,
        CancellationToken cancellationToken)
    {
        if (topics.All(item => item.Action == SeedPlanAction.Unchanged)) return;
        var stored = await dbContext.ContentTopics.ToDictionaryAsync(item => item.Slug,
            StringComparer.Ordinal, cancellationToken);
        var changed = false;
        foreach (var source in topics.Where(item => item.Action != SeedPlanAction.Unchanged))
        {
            if (stored.TryGetValue(source.Slug, out var topic))
            {
                changed |= topic.Rename(source.Name, DateTimeOffset.UtcNow);
                continue;
            }
            dbContext.ContentTopics.Add(ContentTopic.Create(source.Id,
                source.Slug, source.Name, DateTimeOffset.UtcNow));
            changed = true;
        }
        if (!changed) return;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private void Create(LearningContentSeedDocument source, IReadOnlyList<TopicSeedDocument> topics)
    {
        var now = DateTimeOffset.UtcNow;
        var topicIds = TopicIds(source, topics);
        var technologies = source.Technologies!.Select(Parse<Technology>).ToArray();
        Content content;
        if (source is LessonSeedDocument lesson)
        {
            content = Content.CreateDraft(source.Id!.Value, source.Slug!, source.Title!, source.Summary!,
                LearningContentType.Lesson, Parse<ContentDifficulty>(source.Difficulty!),
                source.EstimatedMinutes!.Value, ContentSourceType.Internal, source.Source!.Name!, null,
                technologies, topicIds, ObjectiveInputs(lesson), SectionInputs(lesson), now,
                CandidateInputs(lesson));
        }
        else
        {
            var resource = (ExternalResourceSeedDocument)source;
            content = Content.CreateDraft(source.Id!.Value, source.Slug!, source.Title!, source.Summary!,
                LearningContentType.ExternalResource, Parse<ContentDifficulty>(source.Difficulty!),
                source.EstimatedMinutes!.Value, ContentSourceType.External, source.Source!.Name!, source.Source.Url,
                technologies, topicIds, [], [], now, resourceKind: Parse<ExternalResourceKind>(resource.ResourceKind!));
        }
        content.SetGoals(source.Goals!.Select(Parse<LearningProfileGoal>).ToArray(), now);
        ApplyStatus(content, Parse<ContentStatus>(source.Status!), now);
        dbContext.LearningContents.Add(content);
    }

    private async Task UpdateAsync(LearningContentSeedDocument source,
        IReadOnlyList<TopicSeedDocument> topics, CancellationToken cancellationToken)
    {
        var content = await dbContext.LearningContents.AsSplitQuery()
            .Include(item => item.Technologies).Include(item => item.Topics).Include(item => item.Goals)
            .Include(item => item.Objectives).Include(item => item.Sections).Include(item => item.ReviewCandidates)
            .SingleAsync(item => item.Slug == source.Slug, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (source is LessonSeedDocument lesson)
        {
            var desiredKeys = lesson.ReviewCandidates!.Select(item => item.Key!).ToArray();
            if (content.StageReviewCandidatePositions(desiredKeys))
                await dbContext.SaveChangesAsync(cancellationToken);
            content.SynchronizeLessonEditorial(source.Title!, source.Summary!, ObjectiveInputs(lesson),
                SectionInputs(lesson), CandidateInputs(lesson), now);
            content.ReviseInternalSource(source.Source!.Name!, now);
        }
        else
        {
            var resource = (ExternalResourceSeedDocument)source;
            content.ReviseExternalResource(source.Title!, source.Summary!, source.Source!.Name!, source.Source.Url!,
                Parse<ExternalResourceKind>(resource.ResourceKind!), now);
        }

        content.UpdateLearningMetadata(source.Technologies!.Select(Parse<Technology>).ToArray(),
            Parse<ContentDifficulty>(source.Difficulty!), source.EstimatedMinutes!.Value, now);
        content.SetTopics(TopicIds(source, topics), now);
        content.SetGoals(source.Goals!.Select(Parse<LearningProfileGoal>).ToArray(), now);
        ApplyStatus(content, Parse<ContentStatus>(source.Status!), now);
    }

    private static Guid[] TopicIds(LearningContentSeedDocument source,
        IReadOnlyList<TopicSeedDocument> topics)
    {
        var bySlug = topics.ToDictionary(item => item.Slug!, item => item.Id!.Value, StringComparer.Ordinal);
        return source.Topics!.Select(slug => bySlug[slug]).ToArray();
    }

    private static LearningObjectiveInput[] ObjectiveInputs(LessonSeedDocument lesson) =>
        lesson.Objectives!.Select(value => new LearningObjectiveInput(value)).ToArray();

    private static LearningContentSectionInput[] SectionInputs(LessonSeedDocument lesson) =>
        lesson.Sections!.Select((section, index) => new LearningContentSectionInput(
            Parse<LearningContentSectionType>(section.Type!), section.Heading,
            lesson.ParsedSections[index].BodyMarkdown)).ToArray();

    private static LearningReviewCandidateInput[] CandidateInputs(LessonSeedDocument lesson) =>
        lesson.ReviewCandidates!.Select(item =>
            new LearningReviewCandidateInput(item.Key!, item.Prompt!, item.Answer!)).ToArray();

    private static void ApplyStatus(Content content, ContentStatus desired, DateTimeOffset now)
    {
        if (content.Status == desired) return;
        if (desired == ContentStatus.Published)
        {
            content.Publish(now);
            return;
        }
        if (desired == ContentStatus.Archived)
        {
            if (content.Status == ContentStatus.Draft) content.Publish(now);
            content.Archive(now);
            return;
        }
        throw new InvalidOperationException($"Cannot transition {content.Status} content back to Draft.");
    }

    private static T Parse<T>(string value) where T : struct, Enum => Enum.Parse<T>(value, false);

    private static string SafeFailureMessage(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException)
            return "The catalog item changed concurrently; rerun preview before importing.";
        if (exception is DbUpdateException { InnerException: PostgresException postgres }
            && postgres.ConstraintName is not null && KnownUniqueConstraints.Contains(postgres.ConstraintName))
            return $"Database uniqueness constraint '{postgres.ConstraintName}' rejected the import.";
        if (exception is InvalidOperationException or ArgumentException) return exception.Message;
        return "Database update failed; no changes for this item were committed.";
    }
}
