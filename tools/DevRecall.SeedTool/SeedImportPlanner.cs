using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.SeedTool;

internal static class SeedImportPlanner
{
    public static async Task<SeedImportPlan> BuildAsync(SeedCatalog catalog,
        DevRecallDbContext dbContext, CancellationToken cancellationToken)
    {
        var databaseItems = await dbContext.LearningContents.AsNoTracking().AsSplitQuery()
            .Include(item => item.Technologies).Include(item => item.Topics).Include(item => item.Goals)
            .Include(item => item.Objectives).Include(item => item.Sections).Include(item => item.ReviewCandidates)
            .ToListAsync(cancellationToken);
        var databaseTopics = await dbContext.ContentTopics.AsNoTracking().ToListAsync(cancellationToken);
        var topicSlugs = databaseTopics.ToDictionary(item => item.Id, item => item.Slug);
        var databaseBySlug = databaseItems.ToDictionary(item => item.Slug, StringComparer.Ordinal);
        var sourceSlugs = catalog.Items.Select(item => item.Slug!).ToHashSet(StringComparer.Ordinal);
        var plan = new List<SeedImportPlanItem>();
        var issues = new List<SeedValidationIssue>();

        var databaseTopicsBySlug = databaseTopics.ToDictionary(item => item.Slug, StringComparer.Ordinal);
        foreach (var sourceTopic in catalog.Topics.Where(topic => topic.Slug is not null
                     && databaseTopicsBySlug.ContainsKey(topic.Slug)))
        {
            var stored = databaseTopicsBySlug[sourceTopic.Slug!];
            if (sourceTopic.Id != stored.Id)
                issues.Add(new(SeedIssueSeverity.Error, "SEED_TOPIC_ID_MISMATCH",
                    $"Stored topic ID '{stored.Id}' does not match source ID '{sourceTopic.Id}'.",
                    "seed/taxonomy/topics.json", sourceTopic.Slug));
        }

        foreach (var source in catalog.Items.OrderBy(item => item.Slug, StringComparer.Ordinal))
        {
            if (!databaseBySlug.TryGetValue(source.Slug!, out var existing))
            {
                plan.Add(new(source.Slug!, source.ContentType!, SeedPlanAction.Create, []));
                continue;
            }
            if (source.Id != existing.Id)
            {
                issues.Add(DatabaseError("SEED_ROOT_ID_MISMATCH",
                    $"Stored ID '{existing.Id}' does not match source ID '{source.Id}'.", source));
                continue;
            }
            if (source.ContentType != existing.ContentType.ToString())
            {
                issues.Add(DatabaseError("SEED_CONTENT_TYPE_MISMATCH",
                    "An existing root cannot change content type.", source));
                continue;
            }

            var changes = ChangedGroups(source, existing, topicSlugs, issues);
            plan.Add(new(source.Slug!, source.ContentType!,
                changes.Count == 0 ? SeedPlanAction.Unchanged : SeedPlanAction.Update, changes));
        }

        foreach (var unmanaged in databaseItems.Where(item => !sourceSlugs.Contains(item.Slug))
                     .OrderBy(item => item.Slug, StringComparer.Ordinal))
            issues.Add(new(SeedIssueSeverity.Warning, "SEED_UNMANAGED_DATABASE_CONTENT",
                "Database content is absent from approved source; no action will be taken.",
                "database", unmanaged.Slug));

        return new(plan, issues);
    }

    private static List<string> ChangedGroups(LearningContentSeedDocument source,
        LearningContent existing, IReadOnlyDictionary<Guid, string> topicSlugs,
        List<SeedValidationIssue> issues)
    {
        var changes = new List<string>();
        var sourceDifficulty = Enum.Parse<ContentDifficulty>(source.Difficulty!, false);
        var sourceStatus = Enum.Parse<ContentStatus>(source.Status!, false);
        if (existing.Title != source.Title!.Trim() || existing.Summary != source.Summary!.Trim()
            || existing.Difficulty != sourceDifficulty || existing.EstimatedMinutes != source.EstimatedMinutes
            || existing.Status != sourceStatus)
            changes.Add("Metadata");

        var sourceType = Enum.Parse<ContentSourceType>(source.Source!.Type!, false);
        var resourceKind = source is ExternalResourceSeedDocument resource
            ? Enum.Parse<ExternalResourceKind>(resource.ResourceKind!, false) : (ExternalResourceKind?)null;
        var sourceUrl = NormalizeOptional(source.Source.Url);
        if (existing.SourceType != sourceType || existing.SourceName != source.Source.Name!.Trim()
            || NormalizeOptional(existing.SourceUrl) != sourceUrl || existing.ResourceKind != resourceKind)
            changes.Add("SourceMetadata");

        var technologies = source.Technologies!.Select(value => Enum.Parse<Technology>(value, false));
        if (!SetEqual(technologies, existing.Technologies.Select(item => item.Technology)))
            changes.Add("Technologies");
        var databaseTopics = existing.Topics.Select(item => topicSlugs.GetValueOrDefault(item.TopicId, $"<{item.TopicId}>"));
        if (!SetEqual(source.Topics!, databaseTopics)) changes.Add("Topics");
        var goals = source.Goals!.Select(value => Enum.Parse<LearningProfileGoal>(value, false));
        if (!SetEqual(goals, existing.Goals.Select(item => item.Goal))) changes.Add("Goals");

        if (source is LessonSeedDocument lesson)
        {
            var objectives = existing.Objectives.OrderBy(item => item.Position).Select(item => item.Text).ToArray();
            if (objectives.Length != lesson.Objectives!.Count)
                issues.Add(DatabaseError("SEED_UNSUPPORTED_OBJECTIVE_STRUCTURE",
                    "Existing lesson objective count differs; structural import is deferred.", source));
            else if (!objectives.SequenceEqual(lesson.Objectives.Select(value => value.Trim()), StringComparer.Ordinal))
                changes.Add("Objectives");

            var sections = existing.Sections.OrderBy(item => item.Position).ToArray();
            if (sections.Length != lesson.Sections!.Count)
                issues.Add(DatabaseError("SEED_UNSUPPORTED_SECTION_STRUCTURE",
                    "Existing lesson section count differs; structural import is deferred.", source));
            else if (sections.Where((section, index) => !SectionEqual(section, lesson.Sections[index],
                         lesson.ParsedSections[index])).Any())
                changes.Add("Sections");

            var candidates = existing.ReviewCandidates.OrderBy(item => item.Position).ToArray();
            var sourceCandidates = lesson.ReviewCandidates!;
            if (!SetEqual(candidates.Select(item => item.Key), sourceCandidates.Select(item => item.Key!)))
                issues.Add(DatabaseError("SEED_UNSUPPORTED_CANDIDATE_STRUCTURE",
                    "Existing CandidateKey set differs; candidates cannot be replaced safely.", source));
            else if (CandidateChanges(candidates, sourceCandidates))
                changes.Add("ReviewCandidates");
        }
        return changes;
    }

    private static bool SectionEqual(LearningContentSection existing,
        LessonSectionSeedDocument descriptor, ParsedLessonSection parsed) =>
        existing.SectionType.ToString() == descriptor.Type
        && NormalizeOptional(existing.Heading) == NormalizeOptional(descriptor.Heading)
        && LessonBodyParser.NormalizeLineEndings(existing.BodyMarkdown).Trim() == parsed.BodyMarkdown;

    private static bool CandidateChanges(IReadOnlyList<LearningReviewCandidate> existing,
        List<ReviewCandidateSeedDocument> source)
    {
        var byKey = existing.ToDictionary(item => item.Key, StringComparer.Ordinal);
        for (var position = 0; position < source.Count; position++)
        {
            var candidate = source[position];
            var stored = byKey[candidate.Key!];
            if (stored.Position != position || stored.Prompt != candidate.Prompt!.Trim()
                || stored.Answer != candidate.Answer!.Trim()) return true;
        }
        return false;
    }

    private static bool SetEqual<T>(IEnumerable<T> left, IEnumerable<T> right) where T : notnull =>
        left.ToHashSet().SetEquals(right);
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SeedValidationIssue DatabaseError(string code, string message,
        LearningContentSeedDocument source) => new(SeedIssueSeverity.Error, code, message,
            source.SourcePath, source.Slug);
}
