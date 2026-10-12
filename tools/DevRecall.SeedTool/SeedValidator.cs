using System.Text.RegularExpressions;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;

namespace DevRecall.SeedTool;

internal static partial class SeedValidator
{
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"^(?:q|question)-?\d+$")]
    private static partial Regex PositionalCandidateRegex();

    public static IReadOnlyList<SeedValidationIssue> Validate(SeedCatalog catalog)
    {
        var issues = new List<SeedValidationIssue>(catalog.ReadIssues);
        ValidateTopics(catalog.Topics, issues);
        var topicSlugs = catalog.Topics.Where(topic => topic.Slug is not null)
            .Select(topic => topic.Slug!).ToHashSet(StringComparer.Ordinal);

        foreach (var duplicate in catalog.Items.Where(item => item.Slug is not null)
                     .GroupBy(item => item.Slug!, StringComparer.Ordinal).Where(group => group.Count() > 1))
            foreach (var item in duplicate)
                Error(issues, "SEED_DUPLICATE_SLUG",
                    $"Slug '{duplicate.Key}' appears more than once in the loaded catalog.", item);

        foreach (var duplicate in catalog.Items.Where(item => item.Id is not null)
                     .GroupBy(item => item.Id!.Value).Where(group => group.Count() > 1))
            foreach (var item in duplicate)
                Error(issues, "SEED_DUPLICATE_ID",
                    $"ID '{duplicate.Key}' appears more than once in the loaded catalog.", item);

        foreach (var item in catalog.Items)
        {
            ValidateCommon(item, topicSlugs, issues);
            if (item is LessonSeedDocument lesson) ValidateLesson(lesson, issues);
            else if (item is ExternalResourceSeedDocument resource) ValidateResource(resource, issues);
        }

        foreach (var duplicate in catalog.Items.Where(item => !string.IsNullOrWhiteSpace(item.Title))
                     .GroupBy(item => $"{item.ContentType}:{NormalizeTitle(item.Title!)}", StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
            foreach (var item in duplicate)
                Warning(issues, "SEED_POSSIBLE_DUPLICATE_CONCEPT",
                    $"Another {item.ContentType} has the same normalized title.", item);

        if (catalog.Items.Count > 0 && catalog.Items.All(item => item.Difficulty != nameof(ContentDifficulty.Advanced)))
            issues.Add(new(SeedIssueSeverity.Warning, "SEED_CATALOG_DIFFICULTY_IMBALANCE",
                "Approved catalog has no Advanced content.", "seed/learning-content"));

        return issues.OrderBy(issue => issue.Severity).ThenBy(issue => issue.SourcePath, StringComparer.Ordinal)
            .ThenBy(issue => issue.Code, StringComparer.Ordinal).ToArray();
    }

    private static void ValidateTopics(IReadOnlyList<TopicSeedDocument> topics,
        List<SeedValidationIssue> issues)
    {
        const string path = "seed/taxonomy/topics.json";
        foreach (var topic in topics)
        {
            if (topic.Id is null || topic.Id == Guid.Empty)
                issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_TOPIC_ID",
                    "Every topic requires a non-empty stable ID.", path, topic.Slug));
            if (!IsSlug(topic.Slug))
                issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_TOPIC_SLUG",
                    "Topic slug must be a lowercase hyphenated value.", path, topic.Slug));
            if (string.IsNullOrWhiteSpace(topic.Name) || topic.Name.Trim().Length > 100)
                issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_TOPIC_NAME",
                    "Topic name is required and must not exceed 100 characters.", path, topic.Slug));
        }
        foreach (var group in topics.Where(topic => topic.Slug is not null)
                     .GroupBy(topic => topic.Slug!, StringComparer.Ordinal).Where(group => group.Count() > 1))
            issues.Add(new(SeedIssueSeverity.Error, "SEED_DUPLICATE_TOPIC",
                $"Topic slug '{group.Key}' appears more than once.", path, group.Key));
        foreach (var group in topics.Where(topic => topic.Id is not null)
                     .GroupBy(topic => topic.Id!.Value).Where(group => group.Count() > 1))
            issues.Add(new(SeedIssueSeverity.Error, "SEED_DUPLICATE_TOPIC_ID",
                $"Topic ID '{group.Key}' appears more than once.", path));
    }

    private static void ValidateCommon(LearningContentSeedDocument item,
        HashSet<string> topicSlugs, List<SeedValidationIssue> issues)
    {
        if (item.SchemaVersion != 1)
            Error(issues, "SEED_UNSUPPORTED_SCHEMA_VERSION", "schemaVersion must be 1.", item);
        if (item.Id is null || item.Id == Guid.Empty)
            Error(issues, "SEED_INVALID_ID", "A non-empty stable ID is required.", item);
        if (!IsSlug(item.Slug))
            Error(issues, "SEED_INVALID_SLUG", "Slug must be a lowercase hyphenated value.", item);
        else
        {
            var expectedSlug = item is LessonSeedDocument
                ? Path.GetFileName(Path.GetDirectoryName(item.SourcePath))
                : Path.GetFileNameWithoutExtension(item.SourcePath);
            if (!string.Equals(expectedSlug, item.Slug, StringComparison.Ordinal))
                Error(issues, "SEED_PATH_SLUG_MISMATCH",
                    $"Path identity '{expectedSlug}' must match slug '{item.Slug}'.", item);
        }
        RequiredText(item.Title, 200, "title", item, issues);
        RequiredText(item.Summary, 500, "summary", item, issues);
        ParseEnum<LearningContentType>(item.ContentType, "contentType", item, issues);
        ParseEnum<ContentStatus>(item.Status, "status", item, issues);
        ParseEnum<ContentDifficulty>(item.Difficulty, "difficulty", item, issues);
        if (item.EstimatedMinutes is null or < 1 or > 480)
            Error(issues, "SEED_INVALID_ESTIMATED_MINUTES",
                "estimatedMinutes must be between 1 and 480.", item);
        else if (item.EstimatedMinutes is < 5 or > 90)
            Warning(issues, "SEED_UNUSUAL_ESTIMATED_MINUTES",
                "estimatedMinutes is unusually low or high for portfolio content.", item);

        ValidateEnumSet<Technology>(item.Technologies, "technologies", 20, item, issues);
        ValidateEnumSet<LearningProfileGoal>(item.Goals, "goals", 3, item, issues);
        ValidateStringSet(item.Topics, "topics", 20, item, issues);
        if (item.Topics is not null)
            foreach (var topic in item.Topics.Where(topic => !topicSlugs.Contains(topic)))
                Error(issues, "SEED_UNKNOWN_TOPIC", $"Topic '{topic}' is not canonical.", item);
        if (item.Topics?.Count > 4)
            Warning(issues, "SEED_BROAD_TOPIC_METADATA", "Item has more than four topics.", item);
        if (item.Technologies?.Count > 5)
            Warning(issues, "SEED_BROAD_TECHNOLOGY_METADATA", "Item has more than five technologies.", item);
    }

    private static void ValidateLesson(LessonSeedDocument lesson, List<SeedValidationIssue> issues)
    {
        if (lesson.ContentType != nameof(LearningContentType.Lesson))
            Error(issues, "SEED_INVALID_CONTENT_SHAPE", "Lesson contentType must be 'Lesson'.", lesson);
        if (lesson.Source?.Type != nameof(ContentSourceType.Internal)
            || !string.Equals(lesson.Source.Name?.Trim(), "DevRecall", StringComparison.Ordinal)
            || lesson.Source.Url is not null)
            Error(issues, "SEED_INVALID_SOURCE", "Lesson source must be Internal DevRecall with no URL.", lesson);
        if (string.IsNullOrWhiteSpace(lesson.BodyMarkdown))
            Error(issues, "SEED_MISSING_BODY", "Lesson body.md is required and cannot be blank.", lesson);
        if (lesson.Objectives is null || lesson.Objectives.Count == 0)
            Error(issues, "SEED_MISSING_OBJECTIVES", "Lesson requires at least one objective.", lesson);
        else
        {
            if (lesson.Objectives.Count > 10)
                Error(issues, "SEED_TOO_MANY_OBJECTIVES", "Lesson cannot exceed ten objectives.", lesson);
            else if (lesson.Objectives.Count > 5)
                Warning(issues, "SEED_MANY_OBJECTIVES", "Lesson has more than five objectives.", lesson);
            foreach (var objective in lesson.Objectives)
                RequiredText(objective, 300, "objective", lesson, issues);
        }

        if (lesson.Sections is null || lesson.Sections.Count == 0)
            Error(issues, "SEED_MISSING_SECTIONS", "Lesson requires at least one section.", lesson);
        else
        {
            if (lesson.Sections.Count > 50)
                Error(issues, "SEED_TOO_MANY_SECTIONS", "Lesson cannot exceed fifty sections.", lesson);
            var bodyKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var section in lesson.Sections)
            {
                if (!IsSlug(section.BodyKey) || !bodyKeys.Add(section.BodyKey!))
                    Error(issues, "SEED_INVALID_BODY_KEY", "Section bodyKey must be unique and lowercase hyphenated.", lesson);
                ParseEnum<LearningContentSectionType>(section.Type, "section.type", lesson, issues);
                if (section.Heading is { Length: > 200 })
                    Error(issues, "SEED_FIELD_TOO_LONG", "section.heading cannot exceed 200 characters.", lesson);
            }
            var declared = lesson.Sections.Select(section => section.BodyKey).ToArray();
            var parsed = lesson.ParsedSections.Select(section => section.BodyKey).ToArray();
            if (!declared.SequenceEqual(parsed, StringComparer.Ordinal))
                Error(issues, "SEED_SECTION_MARKER_MISMATCH",
                    "body.md markers must match declared sections exactly and in order.", lesson);
            if (!lesson.Sections.Any(section => section.Type == nameof(LearningContentSectionType.KeyTakeaway)))
                Warning(issues, "SEED_MISSING_KEY_TAKEAWAY", "Lesson has no KeyTakeaway section.", lesson);
        }

        if (lesson.ReviewCandidates is null)
            Error(issues, "SEED_MISSING_REVIEW_CANDIDATES", "reviewCandidates is required.", lesson);
        else
        {
            if (lesson.ReviewCandidates.Count > 5)
                Error(issues, "SEED_TOO_MANY_REVIEW_CANDIDATES", "Lesson cannot exceed five review candidates.", lesson);
            if (lesson.ReviewCandidates.Count < 2)
                Warning(issues, "SEED_FEW_REVIEW_CANDIDATES", "Lesson has fewer than two review candidates.", lesson);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in lesson.ReviewCandidates)
            {
                if (!IsSlug(candidate.Key))
                    Error(issues, "SEED_INVALID_CANDIDATE_KEY",
                        "CandidateKey must be a lowercase hyphenated semantic identity.", lesson);
                else if (!keys.Add(candidate.Key!))
                    Error(issues, "SEED_DUPLICATE_CANDIDATE_KEY",
                        $"CandidateKey '{candidate.Key}' appears more than once.", lesson);
                if (candidate.Key is not null && PositionalCandidateRegex().IsMatch(candidate.Key))
                    Error(issues, "SEED_POSITIONAL_CANDIDATE_KEY",
                        $"CandidateKey '{candidate.Key}' is positional; use a stable concept name.", lesson);
                RequiredText(candidate.Prompt, 500, "reviewCandidate.prompt", lesson, issues);
                RequiredText(candidate.Answer, 2_000, "reviewCandidate.answer", lesson, issues);
            }
        }
    }

    private static void ValidateResource(ExternalResourceSeedDocument resource,
        List<SeedValidationIssue> issues)
    {
        if (resource.ContentType != nameof(LearningContentType.ExternalResource))
            Error(issues, "SEED_INVALID_CONTENT_SHAPE",
                "External resource contentType must be 'ExternalResource'.", resource);
        ParseEnum<ExternalResourceKind>(resource.ResourceKind, "resourceKind", resource, issues);
        if (resource.Source?.Type != nameof(ContentSourceType.External))
            Error(issues, "SEED_INVALID_SOURCE", "External resource source.type must be 'External'.", resource);
        RequiredText(resource.Source?.Name, 150, "source.name", resource, issues);
        var sourceUrl = resource.Source?.Url;
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri)
            || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)
            || (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            || uri.AbsoluteUri.Length > 2_048)
            Error(issues, "SEED_UNSAFE_SOURCE_URL",
                "source.url must be a safe absolute HTTP or HTTPS URL with no user information.", resource);
    }

    private static void ValidateEnumSet<T>(List<string>? values, string field, int maximum,
        LearningContentSeedDocument item, List<SeedValidationIssue> issues) where T : struct, Enum
    {
        if (values is null)
        {
            Error(issues, "SEED_MISSING_FIELD", $"{field} is required.", item);
            return;
        }
        if (values.Count > maximum)
            Error(issues, "SEED_TOO_MANY_VALUES", $"{field} cannot exceed {maximum} values.", item);
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            Error(issues, "SEED_DUPLICATE_VALUE", $"{field} must contain unique values.", item);
        foreach (var value in values) ParseEnum<T>(value, field, item, issues);
    }

    private static void ValidateStringSet(List<string>? values, string field, int maximum,
        LearningContentSeedDocument item, List<SeedValidationIssue> issues)
    {
        if (values is null)
        {
            Error(issues, "SEED_MISSING_FIELD", $"{field} is required.", item);
            return;
        }
        if (values.Count > maximum)
            Error(issues, "SEED_TOO_MANY_VALUES", $"{field} cannot exceed {maximum} values.", item);
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            Error(issues, "SEED_DUPLICATE_VALUE", $"{field} must contain unique values.", item);
        foreach (var value in values.Where(value => !IsSlug(value)))
            Error(issues, "SEED_INVALID_TOPIC_SLUG", $"Topic '{value}' is not a valid slug.", item);
    }

    private static T? ParseEnum<T>(string? value, string field, LearningContentSeedDocument item,
        List<SeedValidationIssue> issues) where T : struct, Enum
    {
        if (value is not null && Enum.TryParse<T>(value, false, out var parsed)
            && Enum.IsDefined(parsed) && !int.TryParse(value, out _)) return parsed;
        Error(issues, "SEED_INVALID_ENUM", $"{field} value '{value ?? "<missing>"}' is not supported.", item);
        return null;
    }

    private static void RequiredText(string? value, int maximum, string field,
        LearningContentSeedDocument item, List<SeedValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
            Error(issues, "SEED_MISSING_FIELD", $"{field} is required.", item);
        else if (value.Trim().Length > maximum)
            Error(issues, "SEED_FIELD_TOO_LONG", $"{field} cannot exceed {maximum} characters.", item);
    }

    private static bool IsSlug(string? value) => value is { Length: <= 160 } && SlugRegex().IsMatch(value);
    private static string NormalizeTitle(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    private static void Error(List<SeedValidationIssue> issues, string code, string message,
        LearningContentSeedDocument item) => issues.Add(new(SeedIssueSeverity.Error, code, message,
            item.SourcePath, item.Slug));
    private static void Warning(List<SeedValidationIssue> issues, string code, string message,
        LearningContentSeedDocument item) => issues.Add(new(SeedIssueSeverity.Warning, code, message,
            item.SourcePath, item.Slug));
}
