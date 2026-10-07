using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Development;

public sealed partial class LearningContentSeeder
{
    // Samples retains the previous authored text as the bounded upgrade baseline, not as another catalog.
    private static List<LearningContentAggregate> CurriculumSamples(DateTimeOffset now, bool includeDogfoodPolish = true)
    {
        var lessons = Samples(now).ToList();
        var bySlug = lessons.ToDictionary(item => item.Slug);
        Revise(bySlug["dependency-injection-fundamentals"], "Dependency Injection Fundamentals",
            "Separate object composition from business behavior with explicit, replaceable dependencies.",
            ["Explain what dependency injection solves.", "Recognize constructor injection.", "Describe why direct dependency creation increases coupling."],
            [(LearningContentSectionType.Explanation, "Composition versus behavior",
                "Imagine an order service that creates its own SQL repository. Changing storage or testing without a database now requires changing that service. Dependency injection moves the choice and construction of collaborators to the application boundary. The service declares what behavior it needs; the caller supplies an implementation. An interface is useful where an alternative implementation or boundary is meaningful, not as mandatory decoration on every class."),
             (LearningContentSectionType.CodeExample, "Before and after",
                "```csharp\n// Before: the service chooses its infrastructure.\npublic sealed class CoupledOrderService\n{\n    private readonly SqlOrderRepository _repository = new();\n}\n\n// After: the service consumes an explicit dependency.\npublic sealed class OrderService(IOrderRepository repository)\n{\n    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken)\n        => repository.FindAsync(id, cancellationToken);\n}\n\n// Application composition boundary:\nservices.AddScoped<IOrderRepository, SqlOrderRepository>();\nservices.AddScoped<OrderService>();\n```\n\nThe example assumes the repository types exist. A test can construct OrderService with a fake repository without changing business code. Registration selects the implementation; it does not replace the need to choose an appropriate lifetime."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Constructor injection makes required dependencies explicit and replaceable. Keep object composition at the application boundary so business behavior does not choose or construct its infrastructure.")],
            [("dependency-injection-purpose", "What problem does dependency injection solve?", "It separates object composition from business behavior, reducing coupling and making dependencies explicit and replaceable."),
             ("constructor-injection", "Why is constructor injection useful?", "Required dependencies are visible and supplied by the caller, so alternative implementations can be used without changing the consuming class.")], now);

        Revise(bySlug["ef-core-tracking-vs-no-tracking"], "EF Core Tracking vs No Tracking",
            "Choose entity tracking for updates and avoid unnecessary tracking in read-only query paths.",
            ["Explain what tracking provides for returned entities.", "Choose no tracking for read-only entity queries.", "Distinguish scalar DTO projections from projections containing entities."],
            [(LearningContentSectionType.Explanation, "Read or update?",
                "Tracking records entity instances in the current DbContext. EF Core can detect changes to those instances and persist them with SaveChanges. Load tracked entities when the use case intends to modify them. If the result is only a read model, decide what must be returned before loading whole entities; tracking is not a substitute for authorization or filtering."),
             (LearningContentSectionType.CodeExample, "Entity reads and DTO projections",
                "```csharp\n// Read-only entities: opt out of change tracking.\nvar orders = await dbContext.Orders.AsNoTracking()\n    .Where(order => order.CustomerId == customerId)\n    .ToListAsync(cancellationToken);\n\n// Scalar DTO only: no entity instance is returned to track.\nvar summaries = await dbContext.Orders\n    .Where(order => order.CustomerId == customerId)\n    .Select(order => new OrderSummary(order.Id, order.Total))\n    .ToListAsync(cancellationToken);\n```\n\nA DTO containing an Order entity can still cause that entity to be tracked by default. The important distinction is whether entity instances are materialized, not whether the outer result type is named DTO. Do not load entities and then project after materialization when SQL can select the required columns."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Use tracking when updating entities through the same DbContext. Use AsNoTracking for read-only entity queries; scalar-only DTO projections already return no entities to track. Filter and project before materialization.")],
            [("tracking-query", "What does an EF Core tracking query provide?", "It records materialized entity instances so changes can be detected and saved through the current DbContext."),
             ("no-tracking-reads", "When should AsNoTracking usually be used?", "Use it for read-only entity queries where the returned entities will not be updated through the same DbContext. A scalar-only DTO projection returns no entity instances to track.")], now);

        Revise(bySlug["aspnet-core-middleware-pipeline"], "ASP.NET Core Middleware Pipeline",
            "Follow nested request/response control flow, reason about middleware order, and recognize short-circuiting.",
            ["Explain how requests flow through middleware and return from downstream components.", "Explain why middleware ordering changes behavior.", "Distinguish terminal middleware from middleware that calls next."],
            [(LearningContentSectionType.Explanation, "A pipeline of decisions",
                "An HTTP request passes through ordered components rather than one giant handler. Each component can inspect or change request state, invoke the next component, and then do work after it returns. Think A before → B before → endpoint → B after → A after for a successful request. Code after next is not guaranteed to run if downstream throws; use try/finally when cleanup must occur."),
             (LearningContentSectionType.Explanation, "What order means",
                "Upstream middleware decides what downstream components receive and whether they run at all. In an explicitly configured security pipeline, authentication establishes the user before authorization checks permissions. An exception handler must wrap the components whose exceptions it handles. UseRouting selects an endpoint; it is not by itself terminal middleware that prevents later static-file handling."),
             (LearningContentSectionType.CodeExample, "Control flow before and after next",
                "```csharp\napp.Use(async (context, next) =>\n{\n    Console.WriteLine(\"A before\");\n    await next();\n    Console.WriteLine(\"A after\");\n});\napp.Use(async (context, next) =>\n{\n    Console.WriteLine(\"B before\");\n    await next();\n    Console.WriteLine(\"B after\");\n});\napp.Run(async context =>\n{\n    Console.WriteLine(\"Endpoint\");\n    await context.Response.WriteAsync(\"Hello\");\n});\n```\n\nThis illustrative trace shows the nesting on a successful request. Run is terminal here: it produces the response without invoking another component."),
             (LearningContentSectionType.Explanation, "Short-circuiting and ordering mistakes",
                "A component that returns without calling next skips the remaining pipeline. This can be intentional, such as serving a static file or rejecting a request, but an early terminal handler can also make later endpoints unreachable. For an explicit security setup, the mental order is routing → authentication → authorization → endpoint. The exact application setup matters; do not mistake a method-name checklist for the control-flow model."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Middleware forms an ordered, nested pipeline. Each component can run before and after next, or short-circuit downstream work. Order controls both the request state later components receive and which components execute.")],
            [("middleware-order-behavior", "Why does ASP.NET Core middleware ordering matter?", "Upstream middleware controls which downstream components run and what request state they receive. For example, authorization needs the identity established by authentication."),
             ("middleware-short-circuiting", "What does short-circuiting an ASP.NET Core request pipeline mean?", "A component handles the request without invoking next, so later middleware and the endpoint do not execute."),
             ("middleware-next-role", "How does control flow work in ASP.NET Core middleware that calls next?", "It runs work before next, awaits the downstream pipeline, and then continues after downstream returns. On a successful request, the response unwinds through the components in reverse order.")], now);

        var asyncLesson = bySlug["async-await-fundamentals"];
        var asyncSections = SectionInputs(asyncLesson);
        asyncSections[0] = new(LearningContentSectionType.Explanation, "Task and await",
            "A Task represents completion of an operation; it may already be complete when returned. Await consumes its result or exception. If it is incomplete, await suspends the async method rather than blocking the calling thread. During genuinely asynchronous I/O in an ASP.NET Core request, the thread can return to the pool while the operation is pending. Awaiting a completed task can continue synchronously; await does not promise a thread switch.");
        asyncSections[^1] = new(LearningContentSectionType.KeyTakeaway, null,
            "Task represents completion; await composes asynchronous work without blocking on an incomplete task. Async does not automatically create another thread. Avoid .Result and .Wait() in request code because blocking can exhaust thread-pool capacity under load.");
        asyncLesson.ReviseLessonText(asyncLesson.Title, asyncLesson.Summary, ObjectiveInputs(asyncLesson), asyncSections, CandidateInputs(asyncLesson), now);

        Revise(bySlug["aspnet-core-cancellation-tokens"], "CancellationToken Fundamentals",
            "Understand cooperative cancellation in .NET and propagate an HTTP caller's cancellation through asynchronous work.",
            ["Explain why cancellation in .NET is cooperative.", "Pass CancellationToken through cancellable asynchronous call chains.", "Distinguish requested cancellation from unexpected application failure."],
            [(LearningContentSectionType.Explanation, "A signal, not a force kill",
                "CancellationToken carries a request to stop. The receiving operation must observe it and stop at a supported safe point; the token does not kill a thread or undo completed writes. A timeout can request cancellation, but callers can cancel for other reasons too. In ASP.NET Core, RequestAborted signals an abandoned request, and endpoint CancellationToken parameters can receive that signal."),
             (LearningContentSectionType.Explanation, "Keep the caller's lifetime",
                "Accept the caller's token and pass it to async APIs that support cancellation. Dropping it or replacing it with CancellationToken.None means abandoned work can continue. If a separate timeout is needed, explicitly link that timeout with caller cancellation rather than ignoring the caller. CPU loops may need occasional ThrowIfCancellationRequested checks; a cancellable I/O API observes the token itself."),
             (LearningContentSectionType.CodeExample, "Request to database",
                "```csharp\napp.MapGet(\"/orders/{id:guid}\", async (Guid id, OrderService service, CancellationToken token)\n    => await service.GetOrderAsync(id, token));\n\npublic sealed class OrderService(AppDbContext db)\n{\n    public Task<Order?> GetOrderAsync(Guid id, CancellationToken token)\n        => db.Orders.SingleOrDefaultAsync(order => order.Id == id, token);\n}\n```\n\nThe types are illustrative. The same token reaches the database API; there is no need to poll it manually beside every await."),
             (LearningContentSectionType.Explanation, "Cancellation versus failure",
                "An operation that observes cancellation commonly reports OperationCanceledException (including TaskCanceledException). When the caller requested cancellation, treat it as that outcome rather than blindly logging an unexpected server failure. Do not swallow it and claim success. An unrelated exception is still a failure, and cancellation is not proof that no side effect occurred. Whether the HTTP client can receive a response after disconnecting is a separate transport concern."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Cancellation is cooperative. Accept the caller's token and propagate it through cancellable async operations so abandoned work can stop. Requested cancellation is distinct from an unexpected failure and does not roll back completed side effects.")],
            [("cancellation-propagation-value", "What should a method normally do with the CancellationToken it receives?", "Pass it to downstream operations that support cancellation, so the caller's request to stop can reach the work actually in progress."),
             ("cancelled-request-continuing-io", "What problem can occur when a request is cancelled but downstream I/O continues?", "Abandoned work still consumes connections and other resources. Cancellation is cooperative: downstream operations must receive and observe the signal to stop."),
             ("cancellation-not-an-error", "Why should requested cancellation not automatically be treated as an unexpected application error?", "OperationCanceledException can represent the caller intentionally abandoning work, not a server defect. Preserve that outcome instead of swallowing it or reporting false success.")], now);
        bySlug["aspnet-core-cancellation-tokens"].UpdateLearningMetadata([Technology.CSharp, Technology.DotNet], ContentDifficulty.Intermediate, 15, now);
        bySlug["aspnet-core-cancellation-tokens"].SetGoals([LearningProfileGoal.ImproveBackendFundamentals], now);

        Revise(bySlug["ef-core-transactions"], "EF Core Transactions",
            "Choose the smallest database transaction boundary that protects a real consistency requirement.",
            ["Explain the normal transactional behavior of SaveChanges with relational providers.", "Decide when an explicit database transaction is needed.", "Explain why database transactions should be short."],
            [(LearningContentSectionType.Explanation, "Start with the consistency requirement",
                "With a provider supporting transactions, one SaveChanges call normally applies its database changes atomically: all succeed or none do. If a workflow can stage its changes and save once, an extra manual transaction usually adds no protection. Ask whether partial persistence would violate a business requirement before choosing a larger boundary."),
             (LearningContentSectionType.Explanation, "Several database steps, one outcome",
                "Multiple SaveChanges calls normally commit separately. An explicit transaction can group them, or group EF changes and SQL using the same database transaction, when partial results must not remain. A remote HTTP request is not automatically a participant: rolling back local data cannot undo an email or a successful payment request. Such workflows need a separate consistency design, not a longer local transaction."),
             (LearningContentSectionType.CodeExample, "Two saves with a deliberate boundary",
                "```csharp\n// Assume both database steps must succeed together.\nawait using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);\ntry\n{\n    order.MarkFulfilled();\n    await db.SaveChangesAsync(cancellationToken);\n    db.FulfillmentLogs.Add(new FulfillmentLog(order.Id));\n    await db.SaveChangesAsync(cancellationToken);\n    await transaction.CommitAsync(cancellationToken);\n}\ncatch\n{\n    // Disposal also rolls back an uncommitted transaction.\n    await transaction.RollbackAsync(CancellationToken.None);\n    throw;\n}\n```"),
             (LearningContentSectionType.Explanation, "Keep the protected boundary small",
                "If both changes can be staged before one SaveChanges, prefer that simpler form; this example illustrates coordination when separate persistence steps are required. Keep slow remote calls and user think-time outside a database transaction: open transactions occupy connections and can hold locks. Configured retrying execution strategies require explicit transaction coordination; do not paste this snippet into a retrying workflow without considering that policy. These are explanatory fragments, not a complete production retry/cleanup implementation."),
             (LearningContentSectionType.KeyTakeaway, null,
                "A normal relational SaveChanges already protects its database changes atomically. Use an explicit transaction only for a wider required database boundary, keep it short, and never assume rollback undoes external HTTP side effects.")],
            [("savechanges-atomicity", "What transaction behavior does EF Core normally provide for one SaveChanges call?", "With a transaction-supporting provider, its database changes are applied atomically: they all commit or are rolled back together."),
             ("explicit-transaction-need", "When can an explicit database transaction be justified in EF Core?", "When multiple database persistence steps must succeed as one unit and cannot be staged into a single SaveChanges call. The steps must participate in the same transaction."),
             ("avoid-unnecessary-transactions", "Why should a single SaveChanges call usually not be wrapped in a manual transaction?", "It already supplies an atomic database boundary. Extra transaction scope adds ceremony and may hold resources longer; keep any required boundary short and do not wait on slow external calls inside it.")], now);
        bySlug["ef-core-transactions"].SetGoals([LearningProfileGoal.ImproveBackendFundamentals], now);

        Revise(bySlug["ef-core-optimistic-concurrency"], "Optimistic Concurrency Fundamentals",
            "Detect lost updates with an expected version and choose a deliberate response to conflicting writes.",
            ["Explain the lost-update problem.", "Explain how an expected version detects a stale write.", "Choose an application response when a concurrency conflict occurs."],
            [(LearningContentSectionType.Explanation, "The lost update comes first",
                "Alice and Bob both read version 4 of an order. Alice saves a change and the row becomes version 5. Bob then submits an edit based on version 4. An unconditional update can silently replace Alice's work. A successful SQL statement is not enough: the application must know whether the data still matches the state on which Bob made his decision."),
             (LearningContentSectionType.Explanation, "Check the expected version at write time",
                "Conceptually, UPDATE orders SET ..., version = 5 WHERE id = ... AND version = 4 succeeds only if the old version still matches. Zero matching rows indicates a stale write or a deleted row, not permission to overwrite. EF Core includes a configured concurrency token's original value in update/delete conditions and reports a conflict with DbUpdateConcurrencyException. Optimistic concurrency does not hold a lock during user think-time; database statements still use their normal locking behavior."),
             (LearningContentSectionType.CodeExample, "An application-managed token",
                "```csharp\n// Mapping: an integer token works without SQL Server rowversion.\nbuilder.Property(order => order.Version).IsConcurrencyToken();\n\n// Order was loaded as a tracked entity with its original Version.\norder.ChangeAddress(address); // Business method also increments Version.\ntry\n{\n    await db.SaveChangesAsync(cancellationToken);\n}\ncatch (DbUpdateConcurrencyException)\n{\n    throw new OrderEditConflictException(order.Id);\n}\n```\n\nThese types are illustrative. Every relevant mutation must advance an application-managed token. For edits sent long after a read, also compare the client's expected version; loading the latest row alone does not detect that stale client decision."),
             (LearningContentSectionType.Explanation, "Resolve, do not erase",
                "A conflict response depends on the use case: reload and ask the user, merge independent changes, or reload and re-apply an operation only if it remains valid. A web API may use 409 Conflict. Retrying the same stale overwrite without reconsidering current state defeats the protection. No single merge/retry policy fits every business action."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Optimistic concurrency detects stale writes by requiring a matching expected version. Surface or resolve the conflict deliberately instead of silently replacing newer data. Token advancement and client expected-version checks are part of that design.")],
            [("lost-update-problem", "What problem does optimistic concurrency protect against?", "A lost update: a writer acting on stale data silently overwrites changes made by another writer after the original read."),
             ("concurrency-token-protection", "How does an expected version detect a stale update?", "The write must match both the row ID and its original version. If another write advanced that version, no row matches and EF Core reports a concurrency conflict."),
             ("conflict-resolution", "What are reasonable responses to a detected concurrency conflict?", "Reload and ask the user, merge compatible changes, or re-apply an operation after validating it against current state. Silently retrying an unchanged stale overwrite is not a resolution.")], now);

        lessons.Add(CreateLesson(Id(109), "aspnet-core-configuration-options", "Configuration and Options in ASP.NET Core",
            "Assumes basic dependency injection: compose configuration providers and give consumers typed, validated settings.",
            ContentDifficulty.Intermediate, 20, [Technology.DotNet, Technology.AspNetCore], [Id(7)],
            ["Explain how later configuration providers can override earlier values.", "Explain why strongly typed options improve maintainability.", "Distinguish IOptions from request-scoped IOptionsSnapshot."],
            [(LearningContentSectionType.Explanation, "One boundary, several providers",
                "Configuration combines values from providers. For a duplicate key, a provider added later takes precedence. In a typical default ASP.NET Core setup, an environment variable can override a JSON setting; Email__Host represents Email:Host. Check the actual provider order rather than assuming a file always wins. Credentials belong in an appropriate secret source, not committed examples."),
             (LearningContentSectionType.Explanation, "Name the dependency",
                "Reading Email:Host string keys throughout application code spreads knowledge of storage format into consumers. Group related settings into EmailOptions at the composition boundary. Consumers then request the values they need, and binding, validation and tests have one clear entry point. Typed objects still need validation: an empty host is not made correct by being a string property."),
             (LearningContentSectionType.CodeExample, "Bind, validate, consume",
                "```csharp\npublic sealed class EmailOptions\n{\n    public string Host { get; set; } = \"\";\n    public int Port { get; set; }\n}\n\nbuilder.Services.AddOptions<EmailOptions>()\n    .Bind(builder.Configuration.GetSection(\"Email\"))\n    .Validate(value => !string.IsNullOrWhiteSpace(value.Host)\n        && value.Port is > 0 and <= 65535, \"Email host/port is invalid\")\n    .ValidateOnStart();\n\npublic sealed class EmailSender(IOptions<EmailOptions> options)\n{\n    private readonly EmailOptions _settings = options.Value;\n}\n```\n\nThis is a binding/consumer fragment, not an email implementation. Validation catches bad settings before an actual send."),
             (LearningContentSectionType.Explanation, "Choose how values refresh",
                "IOptions provides a cached value and does not pick up configuration changes after startup. IOptionsSnapshot is scoped: values are computed when accessed and cached for that scope, usually a request. Later scopes can see updated configuration if the provider supports reload. Do not inject a scoped snapshot into a singleton. IOptionsMonitor can provide current values/change notifications to long-lived consumers; its details are outside this foundation lesson."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Treat configuration as an application boundary. Bind related values to typed, validated options instead of scattering string keys. Choose cached or per-scope settings deliberately; provider reload support determines whether new values are available.")],
            [("typed-options-boundary", "Why prefer strongly typed options over scattered configuration keys?", "They group meaningful settings behind an explicit dependency and centralize binding/validation, making consumers easier to refactor and test."),
             ("configuration-override", "How are duplicate configuration keys resolved across ASP.NET Core providers?", "The provider added later takes precedence for that key. The actual configured provider order determines the winning value."),
             ("options-refresh-scope", "How do IOptions and IOptionsSnapshot differ conceptually?", "IOptions supplies a cached value without reload updates. IOptionsSnapshot caches per scope, so a later request scope can use updated values when the underlying provider supports reload.")], now));

        lessons.Add(CreateLesson(Id(110), "authentication-vs-authorization", "Authentication vs Authorization",
            "Separate identity from permission and protect private resources even when a caller is logged in.",
            ContentDifficulty.Beginner, 15, [Technology.DotNet, Technology.AspNetCore], [Id(8)],
            ["Distinguish authentication from authorization.", "Explain why a valid identity does not grant access to every resource.", "Identify where resource authorization belongs in a request flow."],
            [(LearningContentSectionType.Explanation, "Identity is not permission",
                "Authentication establishes who the caller is using the configured credential mechanism. Authorization decides whether that identity may perform this action on this resource. A logged-in customer can still be forbidden from reading another customer's order. Neither a login screen nor possession of a guessed UUID establishes ownership."),
             (LearningContentSectionType.Explanation, "From request to protected resource",
                "In an explicit ASP.NET Core pipeline, authentication runs before authorization. Roles, claims and policies are mechanisms for permission decisions; resource ownership is another important input. An endpoint requiring an authenticated user is a first gate, not the final check for every row it loads. Apply ownership filtering/checks server-side before returning private data."),
             (LearningContentSectionType.CodeExample, "A private order lookup",
                "```csharp\n// Simplified backend use case; currentUserId comes from trusted auth state.\nvar order = await db.Orders.AsNoTracking()\n    .SingleOrDefaultAsync(order => order.Id == orderId\n        && order.OwnerId == currentUserId, cancellationToken);\nif (order is null)\n    return OrderLookup.NotFound();\nreturn OrderLookup.Found(new OrderSummary(order.Id, order.Total));\n```\n\nThe authenticated caller cannot fetch another owner's order through this lookup. The types are illustrative. The server decides access; hiding a button in the browser does not protect an API."),
             (LearningContentSectionType.Explanation, "Resource-aware decisions",
                "Resource-based authorization evaluates the requested action together with the particular resource, such as ownership of an order or permission to approve it. It may use an authorization handler or scoped query depending on the design. Unauthenticated and authenticated-but-forbidden are different outcomes; APIs often use 401 and 403 respectively, while some hide private resource existence with 404. Choose intentionally and consistently."),
             (LearningContentSectionType.KeyTakeaway, null,
                "Authentication establishes identity; authorization checks an action on a resource. A valid identity can still lack access. Enforce permissions and ownership on the server, not only in navigation or UI controls.")],
            [("identity-vs-permission", "What is the difference between authentication and authorization?", "Authentication establishes who the caller is. Authorization decides whether that caller may perform a particular action on a particular resource."),
             ("authenticated-not-owner", "Why does successful authentication not imply access to every resource?", "Identity alone does not establish ownership or permission. An authenticated user may still be forbidden from reading another account's private data."),
             ("resource-authorization", "What is resource-based authorization?", "A permission decision using the specific resource and requested operation, such as whether a caller owns the order they want to view or can approve it.")], now));

        lessons.Add(CreateLesson(Id(111), "api-error-handling", "API Error Handling",
            "Design predictable error contracts for expected failures without leaking unexpected exception details.",
            ContentDifficulty.Intermediate, 15, [Technology.DotNet, Technology.AspNetCore], [Id(9)],
            ["Distinguish expected application errors from unexpected failures.", "Explain the role of Problem Details and stable machine-readable codes.", "Avoid exposing implementation details or private data in API errors."],
            [(LearningContentSectionType.Explanation, "Errors are part of the contract",
                "Invalid input, a missing resource or a stale edit are ordinary application outcomes. Clients need to know what action is possible: fix input, reload, sign in or stop. An unexpected exception is different: the service could not complete the request as designed. Do not collapse every outcome into 500 or return 200 with a hidden failure flag."),
             (LearningContentSectionType.Explanation, "An intentional representation",
                "Problem Details gives errors a common structure, with status, title, type and optional detail/instance. Validation errors can add field messages; an application can add a stable code. Status expresses the broad HTTP outcome, while a code distinguishes actionable causes within that category. Human text can improve or be localized without forcing clients to parse it as an identifier."),
             (LearningContentSectionType.CodeExample, "A stale-edit response",
                "```json\n{\n  \"type\": \"https://example.com/problems/order-edit-conflict\",\n  \"title\": \"Order changed since it was loaded\",\n  \"status\": 409,\n  \"code\": \"ORDER_EDIT_CONFLICT\",\n  \"traceId\": \"opaque-correlation-id\"\n}\n```\n\nThis is illustrative: the HTTP response status must also be 409. The client can offer reload rather than automatically repeating a stale mutation. The URI identifies an error type; it is not a lesson source URL."),
             (LearningContentSectionType.Explanation, "Keep the boundary safe",
                "Translate known failures consistently at the API boundary: validation, not found, permission and conflict retain meaningful semantics. Central exception handling can give unknown failures a generic 500 response and an opaque correlation ID. Do not send stack traces, SQL, secrets or personal answers to clients. Server diagnostics still require redaction and access control; logging every request body is not a safe substitute. Requested cancellation is not automatically an unexpected error."),
             (LearningContentSectionType.KeyTakeaway, null,
                "API errors should be deliberate contracts. Use HTTP semantics and stable machine-readable error identifiers for expected outcomes; expose safe generic responses for unexpected failures, with correlation rather than private implementation details.")],
            [("expected-vs-unexpected-error", "How do expected API errors differ from unexpected failures?", "Expected errors represent known outcomes such as invalid input or a stale edit, with intentional client actions. Unexpected failures need a safe generic server-error response and diagnostics."),
             ("safe-api-errors", "Why should an API avoid returning raw exception details to clients?", "They can expose stack traces, internal structure or sensitive data. A safe error contract and correlation ID support clients without leaking those details."),
             ("stable-error-codes", "Why are stable machine-readable error codes useful alongside HTTP status codes?", "Status codes classify broad outcomes, while stable application codes distinguish actionable causes without requiring clients to parse changing human messages.")], now));
        if (includeDogfoodPolish)
        {
            var lesson = bySlug["async-await-fundamentals"];
            var sections = SectionInputs(lesson);
            sections[2] = sections[2] with { BodyMarkdown = sections[2].BodyMarkdown.Replace(
                "// await releases the request thread while the database call is in flight.",
                "// If the task is incomplete, await suspends without blocking the request thread.", StringComparison.Ordinal) };
            sections[3] = sections[3] with { BodyMarkdown =
                "Using `.Result` or `.Wait()` to wait for incomplete asynchronous work blocks the calling thread. Under load, many blocked request threads can exhaust thread-pool capacity and increase latency. Await composes that work without holding a thread during the wait. The example also forwards the caller's `CancellationToken` to the repository so a supported operation can stop when cancellation is requested. Cancellation is a related concept, not a required next step or a guarantee that every operation stops immediately." };
            lesson.ReviseLessonText(lesson.Title,
                "Compose asynchronous operations without blocking request threads, and distinguish await from background-thread execution.",
                ObjectiveInputs(lesson), sections, CandidateInputs(lesson), now);
        }
        return lessons;
    }

    private static void Revise(LearningContentAggregate content, string title, string summary, string[] objectives,
        (LearningContentSectionType Type, string? Heading, string Body)[] sections,
        (string Key, string Prompt, string Answer)[] candidates, DateTimeOffset now)
        => content.ReviseLessonText(title, summary, objectives.Select(text => new LearningObjectiveInput(text)).ToArray(),
            sections.Select(item => new LearningContentSectionInput(item.Type, item.Heading, item.Body)).ToArray(),
            candidates.Select(item => new LearningReviewCandidateInput(item.Key, item.Prompt, item.Answer)).ToArray(), now);

    private static LearningObjectiveInput[] ObjectiveInputs(LearningContentAggregate content)
        => content.Objectives.OrderBy(item => item.Position).Select(item => new LearningObjectiveInput(item.Text)).ToArray();
    private static LearningContentSectionInput[] SectionInputs(LearningContentAggregate content)
        => content.Sections.OrderBy(item => item.Position).Select(item => new LearningContentSectionInput(item.SectionType, item.Heading, item.BodyMarkdown)).ToArray();
    private static LearningReviewCandidateInput[] CandidateInputs(LearningContentAggregate content)
        => content.ReviewCandidates.OrderBy(item => item.Position).Select(item => new LearningReviewCandidateInput(item.Key, item.Prompt, item.Answer)).ToArray();
    private static bool SameLessonText(LearningContentAggregate first, LearningContentAggregate second)
        => first.Title == second.Title && first.Summary == second.Summary
            && ObjectiveInputs(first).SequenceEqual(ObjectiveInputs(second))
            && SectionInputs(first).SequenceEqual(SectionInputs(second)) && CandidateInputs(first).SequenceEqual(CandidateInputs(second));
    private static void CopyLessonText(LearningContentAggregate source, LearningContentAggregate target, DateTimeOffset now)
        => target.ReviseLessonText(source.Title, source.Summary, ObjectiveInputs(source), SectionInputs(source), CandidateInputs(source), now);
}
