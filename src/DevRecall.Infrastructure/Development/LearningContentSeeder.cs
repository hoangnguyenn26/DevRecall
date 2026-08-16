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
            (Id(4), "asynchronous-programming", "Asynchronous Programming"),
            (Id(5), "transactions", "Transactions"),
            (Id(6), "concurrency", "Concurrency")
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
                "They block a thread pool thread for the entire wait. Under load, blocked threads accumulate and can starve the thread pool, increasing latency for all requests. await pauses the method without holding a thread.")], now.AddMinutes(1)),
        CreateLesson(Id(106), "aspnet-core-cancellation-tokens", "CancellationToken in ASP.NET Core",
            "Learn how request cancellation flows from the ASP.NET Core request boundary through services and repositories to I/O, and why dropping or replacing the token wastes work.",
            ContentDifficulty.Intermediate, 14, [Technology.AspNetCore, Technology.CSharp], [Id(4)],
            ["Explain where an ASP.NET Core request's CancellationToken originates.",
                "Pass cancellation through the whole call chain down to I/O.",
                "Recognize when a token is dropped or replaced without a reason."],
            [(LearningContentSectionType.Explanation, "Cancellation starts at the request boundary",
                "When a client disconnects or aborts a request, ASP.NET Core signals cancellation through the `HttpContext.RequestAborted` token. Minimal APIs and controllers can accept a `CancellationToken` parameter, and the framework binds it to the current request. The token is a signal, not a force: cooperative code checks it and stops. If application code never looks at the token, the request is still cancelled at the HTTP layer, but the work it started keeps running to the end."),
             (LearningContentSectionType.Explanation, "Propagate the token through the call chain",
                "Cancellation is useful only when it reaches the operation that can actually stop: the database query, the HTTP call, the file read. That means every async method in the request path accepts the token and forwards it to the next call. A service that silently drops the token breaks the chain: the request is gone, but the database call still runs, still holds a connection, and still writes results nobody reads. Creating a brand-new token in the middle of the chain has the same effect, because it is never cancelled when the request aborts. New tokens are for independent work with its own lifetime, such as a timeout around a single outbound call, and that reason should be explicit."),
             (LearningContentSectionType.CodeExample, "One token from request to database",
                "```csharp\napp.MapGet(\"/orders/{id:guid}/summary\", async (\n    Guid id, OrderService service, CancellationToken cancellationToken) =>\n{\n    // cancellationToken is bound to the current request and is cancelled\n    // when the client disconnects or aborts.\n    return await service.GetSummaryAsync(id, cancellationToken);\n});\n\npublic sealed class OrderService(IOrderRepository repository)\n{\n    public async Task<OrderSummary> GetSummaryAsync(Guid orderId, CancellationToken cancellationToken)\n    {\n        // Forward the same token; do not create or drop one here.\n        var order = await repository.GetByIdAsync(orderId, cancellationToken);\n        return new OrderSummary(order.Id, order.Total);\n    }\n}\n```"),
             (LearningContentSectionType.Explanation, "Cancellation is not an error to swallow",
                "When a token is cancelled mid-operation, awaiting it throws `OperationCanceledException`. That is the mechanism working, not a failure to hide. Catching it and continuing, or catching broad exceptions and logging a fake success, turns cancellation into silent wasted work. Let cancellation propagate so the request ends honestly. Also check `cancellationToken.ThrowIfCancellationRequested()` before expensive loops when there is no await to observe the token. This builds directly on async/await: await releases the thread while I/O is in flight, and the cancellation token is how a cancelled request tells that in-flight I/O to stop."),
             (LearningContentSectionType.KeyTakeaway, null,
                "ASP.NET Core exposes request cancellation as a CancellationToken bound to the current request. Pass that same token through every async method down to the database or HTTP call; do not drop it or create a new one mid-chain without an explicit reason. Cancellation surfaces as OperationCanceledException, which should propagate rather than be swallowed, so cancelled requests stop their I/O instead of finishing work nobody is waiting for.")],
            [("cancellation-propagation-value", "Why should an ASP.NET Core request's CancellationToken be passed through to database or HTTP calls?",
                "So those operations can stop when the request is aborted. Without the token, downstream I/O keeps running after the client is gone, wasting connections, CPU and database work whose results are discarded."),
             ("cancelled-request-continuing-io", "What problem can occur when a request is cancelled but downstream I/O continues?",
                "The work still consumes a database connection and server resources, results are produced for nobody, and under load the accumulated abandoned work can degrade the whole application."),
             ("cancellation-not-an-error", "Why should OperationCanceledException usually be allowed to propagate instead of being caught and swallowed?",
                "It is the signal that the request was aborted. Swallowing it makes the code continue doing work nobody is waiting for and can report false success, so cancellation should end the operation honestly.")], now.AddMinutes(2)),
        CreateLesson(Id(107), "ef-core-transactions", "EF Core Transactions",
            "Know when the built-in SaveChanges transaction is enough and when a multi-step workflow needs an explicit transaction boundary.",
            ContentDifficulty.Intermediate, 15, [Technology.EfCore, Technology.DotNet], [Id(5)],
            ["Describe the transaction behavior of a single SaveChanges call.",
                "Identify workflows that need an explicit transaction boundary.",
                "Avoid adding manual transactions where SaveChanges already provides atomicity."],
            [(LearningContentSectionType.Explanation, "SaveChanges is already transactional",
                "A single `SaveChanges` call wraps all of its changes in one database transaction. Every insert, update and delete in that call either commits together or rolls back together. Most application writes fit this shape: load a few aggregates, mutate them through their business methods, call `SaveChanges` once. For those workflows there is nothing extra to manage, and adding a manual transaction would only add ceremony without adding safety."),
             (LearningContentSectionType.Explanation, "When one SaveChanges is not the boundary",
                "Problems appear when a unit of work spans several persistence steps: two `SaveChanges` calls, a mix of EF writes and raw SQL, or database writes combined with an external call. Each `SaveChanges` then commits independently, so a failure halfway leaves earlier changes already persisted. The fix is an explicit transaction around the whole boundary, so every step commits together or none of them does. The question to ask is product-shaped, not API-shaped: if step three fails, may the results of steps one and two remain, or would that leave the data inconsistent?"),
             (LearningContentSectionType.CodeExample, "An explicit transaction boundary",
                "```csharp\nawait using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);\n\norder.MarkFulfilled();\nawait dbContext.SaveChangesAsync(cancellationToken);\n\nawait dbContext.Database.ExecuteSqlInterpolatedAsync(\n    $\"INSERT INTO fulfillment_log (order_id, fulfilled_at) VALUES ({order.Id}, {DateTime.UtcNow})\",\n    cancellationToken);\n\nawait transaction.CommitAsync(cancellationToken);\n```"),
             (LearningContentSectionType.Explanation, "Reading the example",
                "Both the EF Core update and the raw SQL insert sit inside one transaction. If either step throws, nothing is committed and the caller sees a single failure. If the connection drops before `CommitAsync`, the database rolls the transaction back on its own. Notice what the example does not do: it does not wrap a single `SaveChanges` in a manual transaction, and it does not try to make the write and the log insert succeed by retrying silently. The boundary is explicit because the consistency requirement is explicit. Keep boundaries as small as the consistency requirement allows; long transactions hold locks and delay other requests."),
             (LearningContentSectionType.KeyTakeaway, null,
                "A single SaveChanges call already commits all of its changes atomically, so most writes need no manual transaction. Use an explicit transaction only when one unit of work spans multiple persistence steps, such as several SaveChanges calls or EF writes mixed with raw SQL. Decide by asking whether partial results would leave the data inconsistent, and keep the boundary as small as that requirement allows.")],
            [("savechanges-atomicity", "What transaction behavior does a single EF Core SaveChanges call provide?",
                "It wraps all changes in one database transaction, so every insert, update and delete in the call commits together or rolls back together."),
             ("explicit-transaction-need", "When does a workflow need an explicit transaction boundary instead of relying on SaveChanges alone?",
                "When one unit of work spans multiple persistence steps, such as several SaveChanges calls or EF writes mixed with raw SQL, and a failure halfway must not leave earlier steps already committed."),
             ("avoid-unnecessary-transactions", "Why should a single SaveChanges call usually not be wrapped in a manual transaction?",
                "SaveChanges is already atomic, so a manual transaction adds ceremony and can hold locks longer without adding any safety. Add explicit boundaries only where consistency spans multiple steps.")], now.AddMinutes(3)),
        CreateLesson(Id(108), "ef-core-optimistic-concurrency", "Optimistic Concurrency in EF Core",
            "Understand the stale update problem, how a concurrency token detects conflicts, and why a conflict needs an explicit resolution instead of a silent overwrite.",
            ContentDifficulty.Intermediate, 15, [Technology.EfCore, Technology.DotNet], [Id(6)],
            ["Explain how two updates can silently overwrite each other.",
                "Describe how a concurrency token makes EF Core detect the conflict.",
                "Respond to DbUpdateConcurrencyException with an explicit resolution."],
            [(LearningContentSectionType.Explanation, "The stale update problem",
                "Two users read the same record, then both save changes based on what they saw. Without protection, the second write simply overwrites the first, and nobody is told that the data changed in between. The second writer made a decision based on state that no longer exists; the overwrite silently discards the first writer's change. This is a lost update, and it is invisible in logs because every statement succeeded."),
             (LearningContentSectionType.Explanation, "The concurrency token check",
                "Optimistic concurrency assumes conflicts are rare and checks at write time. A column marked as a concurrency token, commonly a `Version` that changes on every update, is included in the `WHERE` clause of the generated `UPDATE`. If another writer changed the row in the meantime, the stored version no longer matches, the update affects zero rows, and EF Core throws `DbUpdateConcurrencyException`. No lock is held while users read or think, which is why this fits interactive applications better than pessimistic locking."),
             (LearningContentSectionType.CodeExample, "Detecting a conflict",
                "```csharp\npublic class StudyPlan\n{\n    public int Version { get; private set; }\n    // Every mutating business method increments Version.\n}\n\n// In the EF Core mapping:\nbuilder.Property(plan => plan.Version).IsConcurrencyToken();\n\n// In the use case:\ntry\n{\n    await dbContext.SaveChangesAsync(cancellationToken);\n}\ncatch (DbUpdateConcurrencyException)\n{\n    // The row changed since it was read. Reload, re-apply or report;\n    // never silently overwrite.\n}\n```"),
             (LearningContentSectionType.Explanation, "A conflict is a decision, not a retry",
                "Catching the exception is not the end; it is the moment a workflow decides what the conflict means. Reload the current state and re-apply the change if it is still valid, merge what both writers changed, or report the conflict so a user chooses. Silently retrying the same write just overwrites the other writer with extra steps. The same pattern protects editable content in DevRecall itself: Knowledge nodes and study plans carry a version that each mutation increments, so a stale edit fails loudly instead of erasing a newer one."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Without protection, a later save silently overwrites changes made after it was read. A concurrency token makes EF Core include the stored version in the UPDATE condition, so a row changed in the meantime updates zero rows and raises DbUpdateConcurrencyException. A conflict then needs an explicit resolution: reload and re-apply, merge, or report. Never let a stale write silently win.")],
            [("lost-update-problem", "What is the stale update problem when two users edit the same record?",
                "Both read the same state and both save. Without protection the second write silently overwrites the first writer's change, because it was based on state that no longer exists."),
             ("concurrency-token-protection", "Why does a concurrency token protect updates from going stale?",
                "EF Core includes the stored token value in the UPDATE's WHERE clause. If the row changed since it was read, the condition matches no rows and EF Core raises DbUpdateConcurrencyException instead of overwriting blindly."),
             ("conflict-resolution", "Why must a concurrency conflict be resolved explicitly instead of silently retrying or overwriting?",
                "The conflict means the data changed under the writer. Silently retrying the same write just overwrites the other change with extra steps, so the workflow must reload and re-apply, merge, or report the conflict for a decision.")], now.AddMinutes(4))
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
