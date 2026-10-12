using System.Text.RegularExpressions;

namespace DevRecall.SeedTool;

internal static partial class LessonBodyParser
{
    [GeneratedRegex(@"(?m)^<!-- devrecall-section: (?<key>[a-z0-9]+(?:-[a-z0-9]+)*) -->\s*$")]
    private static partial Regex MarkerRegex();

    public static (IReadOnlyList<ParsedLessonSection> Sections,
        IReadOnlyList<SeedValidationIssue> Issues) Parse(
        string body, string sourcePath, string? slug)
    {
        var issues = new List<SeedValidationIssue>();
        var matches = MarkerRegex().Matches(body);
        if (matches.Count == 0)
        {
            issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_BODY_STRUCTURE",
                "Lesson body must contain at least one devrecall-section marker.", sourcePath, slug));
            return ([], issues);
        }

        if (!string.IsNullOrWhiteSpace(body[..matches[0].Index]))
            issues.Add(new(SeedIssueSeverity.Error, "SEED_INVALID_BODY_STRUCTURE",
                "Only whitespace may appear before the first section marker.", sourcePath, slug));

        var sections = new List<ParsedLessonSection>(matches.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var key = match.Groups["key"].Value;
            var bodyStart = match.Index + match.Length;
            var bodyEnd = index + 1 < matches.Count ? matches[index + 1].Index : body.Length;
            var sectionBody = NormalizeLineEndings(body[bodyStart..bodyEnd]).Trim();
            if (!keys.Add(key))
                issues.Add(new(SeedIssueSeverity.Error, "SEED_DUPLICATE_BODY_KEY",
                    $"Body key '{key}' is declared more than once.", sourcePath, slug));
            if (sectionBody.Length == 0)
                issues.Add(new(SeedIssueSeverity.Error, "SEED_EMPTY_SECTION_BODY",
                    $"Section '{key}' has an empty Markdown body.", sourcePath, slug));
            sections.Add(new(key, sectionBody));
        }
        return (sections, issues);
    }

    internal static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
