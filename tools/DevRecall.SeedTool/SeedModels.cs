using System.Text.Json.Serialization;

namespace DevRecall.SeedTool;

internal enum SeedIssueSeverity { Error, Warning }
internal enum SeedPlanAction { Create, Update, Unchanged }

internal sealed record SeedValidationIssue(
    SeedIssueSeverity Severity,
    string Code,
    string Message,
    string SourcePath,
    string? Slug = null);

internal sealed record SeedSource(
    string? Type,
    string? Name,
    string? Url = null);

internal abstract class LearningContentSeedDocument
{
    public int? SchemaVersion { get; init; }
    public Guid? Id { get; init; }
    public string? Slug { get; init; }
    public string? Title { get; init; }
    public string? Summary { get; init; }
    public string? ContentType { get; init; }
    public string? Status { get; init; }
    public string? Difficulty { get; init; }
    public int? EstimatedMinutes { get; init; }
    public SeedSource? Source { get; init; }
    public List<string>? Technologies { get; init; }
    public List<string>? Topics { get; init; }
    public List<string>? Goals { get; init; }

    [JsonIgnore]
    public string SourcePath { get; set; } = string.Empty;
}

internal sealed class LessonSeedDocument : LearningContentSeedDocument
{
    public List<string>? Objectives { get; init; }
    public List<LessonSectionSeedDocument>? Sections { get; init; }
    public List<ReviewCandidateSeedDocument>? ReviewCandidates { get; init; }

    [JsonIgnore]
    public string BodyPath { get; set; } = string.Empty;

    [JsonIgnore]
    public string? BodyMarkdown { get; set; }

    [JsonIgnore]
    public IReadOnlyList<ParsedLessonSection> ParsedSections { get; set; } = [];
}

internal sealed class ExternalResourceSeedDocument : LearningContentSeedDocument
{
    public string? ResourceKind { get; init; }
}

internal sealed record LessonSectionSeedDocument(
    string? BodyKey,
    string? Type,
    string? Heading);

internal sealed record ReviewCandidateSeedDocument(
    string? Key,
    string? Prompt,
    string? Answer);

internal sealed record ParsedLessonSection(string BodyKey, string BodyMarkdown);

internal sealed class TopicTaxonomyDocument
{
    public int? SchemaVersion { get; init; }
    public List<TopicSeedDocument>? Topics { get; init; }
}

internal sealed record TopicSeedDocument(Guid? Id, string? Slug, string? Name);

internal sealed record SeedCatalog(
    IReadOnlyList<LearningContentSeedDocument> Items,
    IReadOnlyList<TopicSeedDocument> Topics,
    IReadOnlyList<SeedValidationIssue> ReadIssues);

internal sealed record SeedImportPlanItem(
    string Slug,
    string ContentType,
    SeedPlanAction Action,
    IReadOnlyList<string> ChangedFieldGroups);

internal sealed record SeedImportPlan(
    IReadOnlyList<SeedImportPlanItem> Items,
    IReadOnlyList<SeedValidationIssue> Issues);
