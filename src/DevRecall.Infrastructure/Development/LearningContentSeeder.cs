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
            (Id(2), "change-tracking", "Change Tracking"),
            (Id(3), "request-pipeline", "Request Pipeline"),
            (Id(4), "asynchronous-programming", "Asynchronous Programming")
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
        var sampleBySlug = samples.ToDictionary(item => item.Slug);
        var existingContents = await dbContext.LearningContents.Include(item => item.ReviewCandidates)
            .Where(item => existingSlugs.Contains(item.Slug)).ToListAsync(cancellationToken);
        foreach (var content in existingContents.Where(item => item.ReviewCandidates.Count == 0
            && sampleBySlug.ContainsKey(item.Slug)))
        {
            var sample = sampleBySlug[content.Slug];
            dbContext.LearningContentReviewCandidates.AddRange(sample.ReviewCandidates.Select(candidate =>
                LearningReviewCandidate.Create(Guid.NewGuid(), content.Id, candidate.Position,
                    candidate.Key, candidate.Prompt, candidate.Answer)));
        }
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
                "Constructor injection makes required dependencies explicit and lets the application provide implementations instead of the class constructing them directly. Depend on abstractions that describe required behavior, and keep object composition at the application boundary.")],
            [("dependency-injection-purpose", "What problem does dependency injection solve?",
                "It separates object composition from business behavior, reducing coupling and making dependencies explicit and replaceable."),
             ("constructor-injection", "Why is constructor injection useful?",
                "It makes required dependencies explicit and allows them to be supplied with alternative implementations for testing or change.")], now.AddMinutes(-3)),
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
                "Transient creates a new instance each time it is resolved, Scoped reuses one instance within a request scope, and Singleton reuses one instance for the application lifetime. Choose the shortest lifetime that fits the required state sharing, and never let a Singleton capture a Scoped dependency.")],
            [("three-service-lifetimes", "What are the three built-in ASP.NET Core service lifetimes?",
                "Transient, Scoped, and Singleton."),
             ("scoped-per-request", "How does a Scoped service behave in a typical ASP.NET Core request?",
                "One instance is reused within the request scope, while different requests receive different instances."),
             ("lifetime-selection", "What should guide the lifetime chosen for an ASP.NET Core service?",
                "Choose the shortest lifetime that satisfies the service's state-sharing requirements, then verify that longer-lived services do not capture shorter-lived dependencies."),
             ("captive-dependency", "Why can injecting a Scoped service into a Singleton be problematic?",
                "The Singleton can hold a shorter-lived dependency beyond its intended scope, creating a captive dependency and potentially leaking request-specific state.")], now.AddMinutes(-2)),
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
                "For read models, project only the required columns and use no tracking. Load tracked aggregates when the use case intends to mutate them.")],
            [("tracking-query", "What does an EF Core tracking query do?",
                "It records returned entities in the change tracker so later mutations can be detected and persisted."),
             ("no-tracking-reads", "When should AsNoTracking usually be used?",
                "Use it for read-only queries where the returned entities will not be updated through the current DbContext.")], now.AddMinutes(-1)),
        CreateLesson(Id(104), "aspnet-core-middleware-pipeline", "ASP.NET Core Middleware Pipeline",
            "Understand the request pipeline: why middleware registration order is execution order, and how next and short-circuiting shape request handling.",
            ContentDifficulty.Intermediate, 15, [Technology.AspNetCore, Technology.DotNet], [Id(3)],
            ["Explain what middleware is and where it sits in the request pipeline.",
                "Predict how registration order affects request and response handling.",
                "Recognize short-circuiting when middleware responds without calling next."],
            [(LearningContentSectionType.Explanation, "Where a request is handled",
                "An ASP.NET Core application does not handle requests with one large component. Each request passes through a pipeline of middleware: small components that each see the request and the response. Every component decides either to produce a response itself or to pass the work on. Authentication, routing, exception handling, and static files are all middleware in this pipeline."),
             (LearningContentSectionType.Explanation, "Registration order is execution order",
                "Middleware is registered in `Program.cs`, and the registration order defines the pipeline. The first registered middleware is the outermost layer: it sees the request first and the response last. The last registered middleware sits closest to the endpoint. Reordering registrations changes behavior. An authorization component registered before authentication, for example, has no authenticated user to check."),
             (LearningContentSectionType.CodeExample, "next and short-circuiting",
                "```csharp\napp.Use(async (context, next) =>\n{\n    if (!context.Request.Headers.ContainsKey(\"X-Api-Key\"))\n    {\n        // Short-circuit: respond and skip the rest of the pipeline.\n        context.Response.StatusCode = StatusCodes.Status401Unauthorized;\n        return;\n    }\n\n    // Pass the request deeper into the pipeline, then continue on the way back out.\n    await next();\n});\n```"),
             (LearningContentSectionType.Explanation, "Reading the example",
                "Calling `next()` hands the request to the next middleware; code after `await next()` runs on the way back out along the response path. Returning without calling it short-circuits the pipeline: no later middleware and no endpoint runs. Ordering bugs are often silent. An exception handler registered after the middleware that fails cannot catch the exception, and static file middleware registered after routing never serves files. When request behavior looks wrong, check pipeline order before suspecting the components themselves."),
             (LearningContentSectionType.KeyTakeaway, null,
                "ASP.NET Core processes each request through a pipeline of middleware registered in Program.cs. Registration order is execution order: the first registration sees the request first and the response last. Each component either calls next() to pass work deeper into the pipeline, or returns a response early to short-circuit the remaining components. When request behavior is surprising, check middleware order first.")],
            [("middleware-order-behavior", "Why does changing ASP.NET Core middleware registration order change application behavior?",
                "Registration order defines the nested pipeline: the first registered component runs first on the request and last on the response. Reordering changes which component sees the request first, so behavior such as exception handling or authentication changes with it."),
             ("middleware-short-circuiting", "What happens when middleware writes a response without calling the next delegate?",
                "The rest of the pipeline is skipped, which is called short-circuiting. Later middleware and the endpoint never run, which is how components such as authentication reject requests early."),
             ("middleware-next-role", "What does calling next() do inside ASP.NET Core middleware?",
                "It invokes the next component in the pipeline. Code before the call runs on the request path, and code after awaiting it runs on the response path as the result travels back out.")], now.AddMinutes(1)),
        CreateLesson(Id(105), "async-await-fundamentals", "async/await Fundamentals",
            "Learn what await really does in ASP.NET Core request code: how it frees threads during I/O, why async is not a background thread, and why blocking with .Result is harmful.",
            ContentDifficulty.Beginner, 12, [Technology.CSharp, Technology.DotNet], [Id(4)],
            ["Explain what await does to the flow of an asynchronous method.",
                "Distinguish asynchronous I/O from work running on a background thread.",
                "Explain why .Result and .Wait() are the wrong default in request code."],
            [(LearningContentSectionType.Explanation, "await releases the thread while waiting",
                "An asynchronous method runs synchronously until it reaches an `await` on an operation that is not finished yet. At that point the method pauses and the current thread is released back to the thread pool. In ASP.NET Core that thread can serve another request while the database or HTTP call is in flight. When the operation completes, the method resumes, possibly on a different thread."),
             (LearningContentSectionType.Explanation, "Async is not a background thread",
                "Awaiting a database call does not start a new thread. Asynchronous I/O releases the waiting thread instead of adding parallelism. Background threads and `Task.Run` are tools for CPU-bound work; request I/O does not need them. The value of async on a web server is handling many concurrent requests with one thread pool, not making a single request faster."),
             (LearningContentSectionType.CodeExample, "An asynchronous call chain",
                "```csharp\npublic sealed class OrderService(IOrderRepository repository)\n{\n    public async Task<OrderSummary> GetSummaryAsync(Guid orderId, CancellationToken cancellationToken)\n    {\n        // await releases the request thread while the database call is in flight.\n        var order = await repository.GetByIdAsync(orderId, cancellationToken);\n        return new OrderSummary(order.Id, order.Total);\n    }\n}\n```"),
             (LearningContentSectionType.Explanation, "Why .Result and .Wait() are not the default",
                "Calling `.Result` or `.Wait()` on a task blocks the current thread until the operation completes. The thread sits idle and cannot serve other requests. Under load, blocked threads accumulate and the thread pool can starve: new requests queue up while threads wait. `await` exists to avoid exactly this, because it pauses the method without holding a thread. Notice also that the example passes a `CancellationToken` into the database call so a cancelled request can stop the I/O; the next lesson explains why that token should flow through the whole call chain."),
             (LearningContentSectionType.KeyTakeaway, null,
                "await pauses an asynchronous method and releases the thread while I/O is in flight; the method resumes when the operation completes. Asynchronous I/O does not create background threads. It lets one thread pool serve many concurrent requests. Blocking with .Result or .Wait() holds a thread for the whole wait and can starve the pool under load, so await is the default in ASP.NET Core request code.")],
            [("await-frees-thread", "What happens to an ASP.NET Core request thread while a handler awaits a database call?",
                "The thread is released back to the thread pool and can serve other requests while the I/O is in flight. When the operation completes, the method resumes, possibly on a different thread."),
             ("async-is-not-a-thread", "Does awaiting an asynchronous database call run the work on a background thread?",
                "No. Asynchronous I/O releases the waiting thread instead of starting a new one. Async is about not holding threads during waits, not about running work in parallel."),
             ("sync-over-async-cost", "Why are .Result and .Wait() usually the wrong default in ASP.NET Core request code?",
                "They block a thread pool thread for the entire wait. Under load, blocked threads accumulate and can starve the thread pool, increasing latency for all requests. await pauses the method without holding a thread.")], now.AddMinutes(1))
    ];

    private static LearningContentAggregate CreateLesson(Guid id, string slug, string title, string summary,
        ContentDifficulty difficulty, int minutes, IReadOnlyCollection<Technology> technologies,
        IReadOnlyCollection<Guid> topics, IReadOnlyCollection<string> objectives,
        IReadOnlyCollection<(LearningContentSectionType Type, string? Heading, string Body)> sections,
        IReadOnlyCollection<(string Key, string Prompt, string Answer)> reviewCandidates,
        DateTimeOffset publishedAtUtc)
    {
        var content = LearningContentAggregate.CreateDraft(id, slug, title, summary, LearningContentType.Lesson,
            difficulty, minutes, ContentSourceType.Internal, "DevRecall", null, technologies, topics,
            objectives.Select(value => new LearningObjectiveInput(value)).ToArray(),
            sections.Select(value => new LearningContentSectionInput(value.Type, value.Heading, value.Body)).ToArray(),
            publishedAtUtc.AddMinutes(-1), reviewCandidates.Select(value =>
                new LearningReviewCandidateInput(value.Key, value.Prompt, value.Answer)).ToArray());
        content.Publish(publishedAtUtc);
        return content;
    }

    private static Guid Id(int value) => Guid.Parse($"10000000-0000-0000-0000-{value:D12}");
}
