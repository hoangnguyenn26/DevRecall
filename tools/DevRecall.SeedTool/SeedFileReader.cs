using System.Text.Json;

namespace DevRecall.SeedTool;

internal sealed class SeedFileReader(string repositoryRoot)
{
    private readonly string _seedRoot = Path.Combine(repositoryRoot, "seed");

    public SeedCatalog Read(bool includeDrafts = false)
    {
        var items = new List<LearningContentSeedDocument>();
        var issues = new List<SeedValidationIssue>();
        var roots = new List<string> { Path.Combine(_seedRoot, "learning-content") };
        if (includeDrafts) roots.Add(Path.Combine(_seedRoot, "drafts"));

        foreach (var root in roots)
        {
            ReadLessons(Path.Combine(root, "lessons"), items, issues);
            ReadResources(Path.Combine(root, "resources"), items, issues);
        }

        var topics = ReadTopics(issues);
        return new(items.OrderBy(item => item.Slug, StringComparer.Ordinal).ToArray(),
            topics.OrderBy(topic => topic.Slug, StringComparer.Ordinal).ToArray(), issues);
    }

    private void ReadLessons(string directory, List<LearningContentSeedDocument> items,
        List<SeedValidationIssue> issues)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var lessonDirectory in Directory.EnumerateDirectories(directory)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var metadataPath = Path.Combine(lessonDirectory, "content.json");
            var bodyPath = Path.Combine(lessonDirectory, "body.md");
            if (!File.Exists(metadataPath))
            {
                issues.Add(new(SeedIssueSeverity.Error, "SEED_MISSING_METADATA",
                    "Lesson directory must contain content.json.", Relative(lessonDirectory)));
                continue;
            }

            var document = ReadDocument(metadataPath, issues);
            if (document is not LessonSeedDocument lesson)
            {
                if (document is not null)
                    issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_CONTENT_SHAPE",
                        "Files under lessons must declare contentType 'Lesson'.",
                        Relative(metadataPath), document.Slug));
                continue;
            }

            lesson.BodyPath = Relative(bodyPath);
            if (!File.Exists(bodyPath))
                issues.Add(new(SeedIssueSeverity.Error, "SEED_MISSING_BODY",
                    "Lesson directory must contain body.md.", Relative(bodyPath), lesson.Slug));
            else
            {
                try
                {
                    lesson.BodyMarkdown = File.ReadAllText(bodyPath);
                    var parsed = LessonBodyParser.Parse(lesson.BodyMarkdown, lesson.BodyPath, lesson.Slug);
                    lesson.ParsedSections = parsed.Sections;
                    issues.AddRange(parsed.Issues);
                }
                catch (IOException exception)
                {
                    issues.Add(ReadFailure(bodyPath, lesson.Slug, exception));
                }
            }
            items.Add(lesson);
        }
    }

    private void ReadResources(string directory, List<LearningContentSeedDocument> items,
        List<SeedValidationIssue> issues)
    {
        if (!Directory.Exists(directory)) return;
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            var document = ReadDocument(path, issues);
            if (document is not ExternalResourceSeedDocument resource)
            {
                if (document is not null)
                    issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_CONTENT_SHAPE",
                        "Files under resources must declare contentType 'ExternalResource'.",
                        Relative(path), document.Slug));
                continue;
            }
            items.Add(resource);
        }
    }

    private LearningContentSeedDocument? ReadDocument(string path,
        List<SeedValidationIssue> issues)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var parsed = JsonDocument.Parse(json);
            var contentType = parsed.RootElement.TryGetProperty("contentType", out var typeElement)
                && typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : null;
            LearningContentSeedDocument? document = contentType switch
            {
                "Lesson" => JsonSerializer.Deserialize<LessonSeedDocument>(json, SeedJson.Options),
                "ExternalResource" => JsonSerializer.Deserialize<ExternalResourceSeedDocument>(json, SeedJson.Options),
                _ => null
            };
            if (document is null)
            {
                issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_CONTENT_TYPE",
                    "contentType must be 'Lesson' or 'ExternalResource'.", Relative(path)));
                return null;
            }
            document.SourcePath = Relative(path);
            return document;
        }
        catch (JsonException exception)
        {
            issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_JSON",
                $"JSON could not be read: {exception.Message}", Relative(path)));
        }
        catch (IOException exception)
        {
            issues.Add(ReadFailure(path, null, exception));
        }
        return null;
    }

    private List<TopicSeedDocument> ReadTopics(List<SeedValidationIssue> issues)
    {
        var path = Path.Combine(_seedRoot, "taxonomy", "topics.json");
        if (!File.Exists(path))
        {
            issues.Add(new(SeedIssueSeverity.Error, "SEED_MISSING_TAXONOMY",
                "seed/taxonomy/topics.json was not found.", Relative(path)));
            return [];
        }
        try
        {
            var document = JsonSerializer.Deserialize<TopicTaxonomyDocument>(
                File.ReadAllText(path), SeedJson.Options);
            if (document?.SchemaVersion != 1)
                issues.Add(new(SeedIssueSeverity.Error, "SEED_UNSUPPORTED_SCHEMA_VERSION",
                    "Topic taxonomy schemaVersion must be 1.", Relative(path)));
            return document?.Topics ?? [];
        }
        catch (JsonException exception)
        {
            issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_JSON",
                $"Topic taxonomy JSON could not be read: {exception.Message}", Relative(path)));
        }
        catch (IOException exception)
        {
            issues.Add(ReadFailure(path, null, exception));
        }
        return [];
    }

    private SeedValidationIssue ReadFailure(string path, string? slug, Exception exception) =>
        new(SeedIssueSeverity.Error, "SEED_FILE_READ_FAILED",
            $"File could not be read: {exception.Message}", Relative(path), slug);

    private string Relative(string path) => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
}
