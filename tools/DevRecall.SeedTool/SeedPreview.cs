namespace DevRecall.SeedTool;

internal static class SeedPreview
{
    public static void Write(SeedCatalog catalog, IReadOnlyList<SeedValidationIssue> issues)
    {
        var lessons = catalog.Items.OfType<LessonSeedDocument>().ToArray();
        var resources = catalog.Items.OfType<ExternalResourceSeedDocument>().ToArray();
        Console.WriteLine("DevRecall Portfolio Dataset Preview");
        Console.WriteLine();
        Console.WriteLine("Catalog");
        Row("Lessons", lessons.Length);
        Row("External resources", resources.Length);
        Row("Total", catalog.Items.Count);
        Distribution("Difficulty", catalog.Items.Select(item => item.Difficulty));
        Distribution("Technology coverage", catalog.Items.SelectMany(item => item.Technologies ?? []));
        Distribution("Topic coverage", catalog.Items.SelectMany(item => item.Topics ?? []));
        Distribution("Goal coverage", catalog.Items.SelectMany(item => item.Goals ?? []));

        Console.WriteLine();
        Console.WriteLine("Lesson review candidates");
        Row("Total candidates", lessons.Sum(item => item.ReviewCandidates?.Count ?? 0));
        Row("Average per lesson", lessons.Length == 0 ? 0
            : Math.Round(lessons.Average(item => item.ReviewCandidates?.Count ?? 0), 1));

        var lowTopics = catalog.Topics.Select(topic => topic.Slug)
            .Where(slug => slug is not null)
            .Select(slug => new { Slug = slug!, Count = catalog.Items.Count(item => item.Topics?.Contains(slug!) == true) })
            .Where(item => item.Count <= 1).OrderBy(item => item.Slug, StringComparer.Ordinal).ToArray();
        if (lowTopics.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Low topic coverage");
            foreach (var item in lowTopics) Row(item.Slug, item.Count);
        }

        var warnings = issues.Where(issue => issue.Severity == SeedIssueSeverity.Warning).ToArray();
        Console.WriteLine();
        Console.WriteLine("Warnings");
        Row("Total", warnings.Length);
        foreach (var group in warnings.GroupBy(issue => issue.Code).OrderBy(group => group.Key, StringComparer.Ordinal))
            Row(group.Key, group.Count());
    }

    private static void Distribution(string heading, IEnumerable<string?> values)
    {
        Console.WriteLine();
        Console.WriteLine(heading);
        foreach (var group in values.Where(value => !string.IsNullOrWhiteSpace(value))
                     .GroupBy(value => value!, StringComparer.Ordinal)
                     .OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal))
            Row(group.Key, group.Count());
    }

    private static void Row(string label, object value) => Console.WriteLine($"{label,-32}{value,6}");
}
