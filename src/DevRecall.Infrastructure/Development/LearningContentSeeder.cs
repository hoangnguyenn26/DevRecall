using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Development;

public sealed class LearningContentSeeder(DevRecallDbContext dbContext)
{
    public static void EnsureDevelopmentEnvironment(bool isDevelopment)
    {
        if (!isDevelopment)
            throw new InvalidOperationException("Learning Content can only be seeded in the Development environment.");
    }

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var topicDefinitions = new[]
        {
            (Id(1), "dependency-injection", "Dependency Injection"),
            (Id(2), "change-tracking", "Change Tracking")
        };
        var existingTopicSlugs = await dbContext.ContentTopics.AsNoTracking()
            .Select(item => item.Slug).ToListAsync(cancellationToken);
        foreach (var definition in topicDefinitions.Where(item => !existingTopicSlugs.Contains(item.Item2)))
            dbContext.ContentTopics.Add(ContentTopic.Create(definition.Item1, definition.Item2, definition.Item3, now));

        var existingSlugs = await dbContext.LearningContents.AsNoTracking()
            .Select(item => item.Slug).ToListAsync(cancellationToken);
        var samples = Samples(now);
        var additions = samples.Where(item => !existingSlugs.Contains(item.Slug)).ToArray();
        dbContext.LearningContents.AddRange(additions);
        if (dbContext.ChangeTracker.HasChanges()) await dbContext.SaveChangesAsync(cancellationToken);
        return additions.Length;
    }

    private static IReadOnlyList<LearningContentAggregate> Samples(DateTimeOffset now) =>
    [
        CreateLesson(Id(101), "dependency-injection-fundamentals", "Dependency Injection Fundamentals",
            "Understand why dependency injection reduces coupling and how constructor injection makes dependencies explicit.",
            ContentDifficulty.Beginner, 12, [Technology.AspNetCore], [Id(1)],
            ["Explain what dependency injection solves.", "Recognize constructor injection.",
                "Describe why direct dependency creation increases coupling."],
            [(LearningContentSectionType.Explanation, "The dependency problem",
                "Classes that construct infrastructure dependencies directly become hard to change and test. Dependency injection moves object composition to the application boundary."),
             (LearningContentSectionType.CodeExample, "Constructor injection",
                "```csharp\npublic sealed class OrderService(IOrderRepository repository)\n{\n    // The dependency is explicit and replaceable.\n}\n```"),
             (LearningContentSectionType.KeyTakeaway, null,
                "Depend on an abstraction that represents required behavior; keep composition outside the class that uses it.")], now.AddMinutes(-3)),
        CreateLesson(Id(102), "aspnet-core-service-lifetimes", "ASP.NET Core Service Lifetimes",
            "Choose between Transient, Scoped, and Singleton services and avoid lifetime dependency mistakes.",
            ContentDifficulty.Intermediate, 15, [Technology.AspNetCore, Technology.DotNet], [Id(1)],
            ["Explain the three built-in service lifetimes.", "Choose an appropriate lifetime for a service.",
                "Identify a captive dependency."],
            [(LearningContentSectionType.Explanation, "Transient, Scoped, and Singleton",
                "Transient creates a new instance when resolved. Scoped reuses one instance within a request scope. Singleton reuses one instance for the application's lifetime."),
             (LearningContentSectionType.CodeExample, "Registration",
                "```csharp\nservices.AddTransient<IFormatter, Formatter>();\nservices.AddScoped<IOrderService, OrderService>();\nservices.AddSingleton<ISystemClock, SystemClock>();\n```"),
             (LearningContentSectionType.Explanation, "Lifetime mismatch",
                "A singleton must not capture a scoped dependency. Doing so extends the scoped object's effective lifetime and can leak request-specific state."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Choose the shortest lifetime that fits the state and sharing requirements, then validate the dependency graph.")], now.AddMinutes(-2)),
        CreateLesson(Id(103), "ef-core-tracking-vs-no-tracking", "EF Core Tracking vs No Tracking",
            "Understand change tracking, AsNoTracking, and the right default for read-only query paths.",
            ContentDifficulty.Intermediate, 15, [Technology.EfCore, Technology.DotNet], [Id(2)],
            ["Explain what a tracking query does.", "Use AsNoTracking for read-only projections.",
                "Recognize when tracking is required for updates."],
            [(LearningContentSectionType.Explanation, "Tracking queries",
                "EF Core records tracked entities in the change tracker so later mutations can be detected and persisted."),
             (LearningContentSectionType.CodeExample, "Read-only query",
                "```csharp\nvar orders = await dbContext.Orders\n    .AsNoTracking()\n    .Where(order => order.CustomerId == customerId)\n    .Select(order => new OrderSummary(order.Id, order.Total))\n    .ToListAsync(cancellationToken);\n```"),
             (LearningContentSectionType.KeyTakeaway, null,
                "For read models, project only the required columns and use no tracking. Load tracked aggregates when the use case intends to mutate them.")], now.AddMinutes(-1))
    ];

    private static LearningContentAggregate CreateLesson(Guid id, string slug, string title, string summary,
        ContentDifficulty difficulty, int minutes, IReadOnlyCollection<Technology> technologies,
        IReadOnlyCollection<Guid> topics, IReadOnlyCollection<string> objectives,
        IReadOnlyCollection<(LearningContentSectionType Type, string? Heading, string Body)> sections,
        DateTimeOffset publishedAtUtc)
    {
        var content = LearningContentAggregate.CreateDraft(id, slug, title, summary, LearningContentType.Lesson,
            difficulty, minutes, ContentSourceType.Internal, "DevRecall", null, technologies, topics,
            objectives.Select(value => new LearningObjectiveInput(value)).ToArray(),
            sections.Select(value => new LearningContentSectionInput(value.Type, value.Heading, value.Body)).ToArray(),
            publishedAtUtc.AddMinutes(-1));
        content.Publish(publishedAtUtc);
        return content;
    }

    private static Guid Id(int value) => Guid.Parse($"10000000-0000-0000-0000-{value:D12}");
}
