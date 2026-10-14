using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.SeedTool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            WriteUsage();
            return args.Length == 0 ? 2 : 0;
        }

        var root = FindRepositoryRoot(Environment.CurrentDirectory);
        if (root is null)
        {
            Console.Error.WriteLine("Could not find DevRecall.slnx from the current directory.");
            return 2;
        }

        var command = args[0];
        if (command == "demo") return await PortfolioDemoSeeder.RunAsync();
        var includeDrafts = command == "validate"
            && args.Contains("--include-drafts", StringComparer.Ordinal);
        var reader = new SeedFileReader(root);
        var catalog = reader.Read(includeDrafts);
        var issues = SeedValidator.Validate(catalog);

        return command switch
        {
            "validate" => Validate(issues),
            "preview" => Preview(catalog, issues),
            "import" => await ImportAsync(args, catalog, issues),
            _ => Unknown(command)
        };
    }

    private static int Validate(IReadOnlyList<SeedValidationIssue> issues)
    {
        WriteIssues(issues);
        var errors = issues.Count(issue => issue.Severity == SeedIssueSeverity.Error);
        var warnings = issues.Count - errors;
        Console.WriteLine($"Validation complete: {errors} error(s), {warnings} warning(s).");
        return errors == 0 ? 0 : 1;
    }

    private static int Preview(SeedCatalog catalog, IReadOnlyList<SeedValidationIssue> issues)
    {
        if (issues.Any(issue => issue.Severity == SeedIssueSeverity.Error))
        {
            WriteIssues(issues);
            Console.Error.WriteLine("Preview stopped because validation failed.");
            return 1;
        }
        SeedPreview.Write(catalog, issues);
        return 0;
    }

    private static async Task<int> ImportAsync(string[] args, SeedCatalog catalog,
        IReadOnlyList<SeedValidationIssue> validationIssues)
    {
        var dryRun = args.Contains("--dry-run", StringComparer.Ordinal);
        if (validationIssues.Any(issue => issue.Severity == SeedIssueSeverity.Error))
        {
            WriteIssues(validationIssues);
            Console.Error.WriteLine("Import stopped because validation failed.");
            return 1;
        }

        var requestedSlug = OptionValue(args, "--slug");
        if (args.Contains("--slug", StringComparer.Ordinal) && requestedSlug is null)
        {
            Console.Error.WriteLine("--slug requires a value.");
            return 2;
        }
        if (requestedSlug is not null && catalog.Items.All(item => item.Slug != requestedSlug))
        {
            Console.Error.WriteLine($"Approved source does not contain slug '{requestedSlug}'.");
            return 2;
        }
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("Set ConnectionStrings__Database before running import.");
            return 2;
        }

        var options = new DbContextOptionsBuilder<DevRecallDbContext>()
            .UseNpgsql(connectionString).UseSnakeCaseNamingConvention().Options;
        await using var dbContext = new DevRecallDbContext(options);
        var plan = await SeedImportPlanner.BuildAsync(catalog, dbContext, CancellationToken.None);
        var combinedIssues = validationIssues.Concat(plan.Issues).ToArray();
        WriteIssues(combinedIssues.Where(issue => issue.Severity == SeedIssueSeverity.Warning));
        if (plan.Issues.Any(issue => issue.Severity == SeedIssueSeverity.Error))
        {
            WriteIssues(plan.Issues.Where(issue => issue.Severity == SeedIssueSeverity.Error));
            Console.Error.WriteLine("Import stopped because database comparison found unsafe changes.");
            return 1;
        }

        var items = requestedSlug is null ? plan.Items
            : plan.Items.Where(item => item.Slug == requestedSlug).ToArray();
        if (dryRun)
        {
            var selectedTopicSlugs = items.SelectMany(item => catalog.Items.Single(source => source.Slug == item.Slug).Topics!)
                .ToHashSet(StringComparer.Ordinal);
            WritePlan(items, plan.Topics.Where(item => selectedTopicSlugs.Contains(item.Slug)).ToArray());
            return 0;
        }

        var importer = new SeedCatalogImporter(dbContext);
        var result = await importer.ApplyAsync(catalog, plan, requestedSlug, CancellationToken.None);
        WriteImportResult(result);
        return result.Failures.Count == 0 ? 0 : 1;
    }

    private static void WriteImportResult(SeedImportResult result)
    {
        Console.WriteLine("DevRecall Portfolio Dataset Import");
        foreach (var action in Enum.GetValues<SeedPlanAction>())
        {
            Console.WriteLine();
            Console.WriteLine(action.ToString().ToUpperInvariant());
            foreach (var item in result.AppliedItems.Where(item => item.Action == action))
            {
                var symbol = action switch { SeedPlanAction.Create => "+", SeedPlanAction.Update => "~", _ => "=" };
                Console.WriteLine($"{symbol} {item.Slug} [{item.ContentType}]");
                if (item.ChangedFieldGroups.Count > 0)
                    Console.WriteLine($"  {string.Join(", ", item.ChangedFieldGroups)}");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Summary");
        foreach (var action in Enum.GetValues<SeedPlanAction>())
            Console.WriteLine($"{action,-12}{result.AppliedItems.Count(item => item.Action == action),5}");
        Console.WriteLine($"{"Failed",-12}{result.Failures.Count,5}");
        Console.WriteLine($"Topics      {result.AppliedTopics.Count,5} ({result.AppliedTopics.Count(item => item.Action != SeedPlanAction.Unchanged)} changed)");
        foreach (var failure in result.Failures)
            Console.Error.WriteLine($"FAILED {failure.Slug}: {failure.Message}");
    }

    private static void WritePlan(IReadOnlyList<SeedImportPlanItem> items, SeedTopicPlanItem[] topics)
    {
        Console.WriteLine("Import plan");
        foreach (var action in Enum.GetValues<SeedPlanAction>())
        {
            var actionItems = items.Where(item => item.Action == action).ToArray();
            Console.WriteLine();
            Console.WriteLine(action.ToString().ToUpperInvariant());
            foreach (var item in actionItems)
            {
                var symbol = action switch { SeedPlanAction.Create => "+", SeedPlanAction.Update => "~", _ => "=" };
                Console.WriteLine($"{symbol} {item.Slug} [{item.ContentType}]");
                if (item.ChangedFieldGroups.Count > 0)
                    Console.WriteLine($"  {string.Join(", ", item.ChangedFieldGroups)}");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Summary");
        foreach (var action in Enum.GetValues<SeedPlanAction>())
            Console.WriteLine($"{action,-12}{items.Count(item => item.Action == action),5}");
        Console.WriteLine($"Topics      {topics.Length,5} ({topics.Count(item => item.Action != SeedPlanAction.Unchanged)} changed)");
        Console.WriteLine();
        Console.WriteLine("Database writes: 0");
    }

    private static void WriteIssues(IEnumerable<SeedValidationIssue> issues)
    {
        foreach (var issue in issues)
        {
            var slug = issue.Slug is null ? string.Empty : $" [{issue.Slug}]";
            Console.WriteLine($"{issue.Severity.ToString().ToUpperInvariant()} {issue.Code} "
                + $"{issue.SourcePath}{slug}: {issue.Message}");
        }
    }

    private static string? OptionValue(string[] args, string option)
    {
        var index = Array.IndexOf(args, option);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command '{command}'.");
        WriteUsage();
        return 2;
    }

    private static string? FindRepositoryRoot(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DevRecall.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }

    private static void WriteUsage()
    {
        Console.WriteLine("DevRecall portfolio dataset tooling");
        Console.WriteLine("  validate [--include-drafts]");
        Console.WriteLine("  preview");
        Console.WriteLine("  import [--dry-run] [--slug <slug>]");
        Console.WriteLine("  demo (Development only; requires ConnectionStrings__Database and PortfolioDemo__Password)");
    }
}
