using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Content = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.SeedTool;

// One synthetic portfolio persona, separate from both the catalog and personal playground.
internal static class PortfolioDemoSeeder
{
    internal const string Email = "portfolio@devrecall.local";
    private static Guid Id(int value) => Guid.Parse($"d0900000-0000-0000-0000-{value:D12}");

    private static readonly (string Slug, int DaysAgo, string[] Keys, string Note)[] Completed =
    [
        ("dependency-injection-fundamentals", 22, ["dependency-injection-purpose", "constructor-injection"],
            "Keep object composition at the application boundary. Constructor parameters make required dependencies visible and replaceable in tests; avoid constructing infrastructure inside business logic."),
        ("aspnet-core-service-lifetimes", 18, ["three-service-lifetimes", "captive-dependency"],
            "A scoped dependency belongs to a request, not the whole application. A singleton capturing it can retain request state. Check the entire dependency chain when choosing a lifetime."),
        ("async-await-fundamentals", 14, ["async-is-not-a-thread", "sync-over-async-cost"],
            "Await asynchronous I/O instead of blocking with Result or Wait. Async composition does not promise a new thread; it allows the waiting thread to do other work."),
        ("ef-core-tracking-vs-no-tracking", 10, ["tracking-query", "no-tracking-reads"],
            "Use tracking when entities will be edited through the same DbContext. Prefer no-tracking for read-only entity queries; scalar DTO projections do not materialize tracked entities."),
        ("aspnet-core-middleware-pipeline", 7, ["middleware-order-behavior", "middleware-short-circuiting"],
            "Middleware wraps downstream work. Authentication must establish identity before authorization evaluates it. A component that never calls next prevents later middleware and the endpoint from running."),
        ("aspnet-core-cancellation-tokens", 4, [], "")
    ];

    internal static async Task<int> RunAsync()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") != "Development")
        {
            Console.Error.WriteLine("Demo seeding requires DOTNET_ENVIRONMENT=Development. Never run against production.");
            return 2;
        }
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database");
        var password = Environment.GetEnvironmentVariable("PortfolioDemo__Password");
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            Console.Error.WriteLine("Set ConnectionStrings__Database and PortfolioDemo__Password (at least 12 characters).");
            return 2;
        }
        var options = new DbContextOptionsBuilder<DevRecallDbContext>().UseNpgsql(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new DevRecallDbContext(options);
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2026101401)");
            var existing = await db.Users.SingleOrDefaultAsync(x => x.Id == Id(1) || x.NormalizedEmail == UserEmail.Normalize(Email));
            if (existing is not null)
            {
                if (existing.Id != Id(1) || existing.Email != Email)
                    throw new InvalidOperationException("Portfolio account identity collision; no data changed.");
                Console.WriteLine($"Unchanged: {Email}. Existing activity, anchor time and password preserved; writes: 0.");
                return 0;
            }

            var catalog = await db.LearningContents.AsNoTracking().Include(x => x.ReviewCandidates).ToListAsync();
            Content Require(string slug, LearningContentType type) => catalog.SingleOrDefault(x => x.Slug == slug
                && x.ContentType == type && x.Status == ContentStatus.Published)
                ?? throw new InvalidOperationException($"Required Published {type} '{slug}' is missing; import the approved catalog first.");
            var lessons = Completed.Select(x => Require(x.Slug, LearningContentType.Lesson)).ToArray();
            var ongoing = Require("ef-core-transactions", LearningContentType.Lesson);
            var next = Require("ef-core-optimistic-concurrency", LearningContentType.Lesson);
            var resource = Require("ef-core-transactions-documentation", LearningContentType.ExternalResource);
            var historyResource = Require("ef-core-tracking-documentation", LearningContentType.ExternalResource);
            for (var i = 0; i < Completed.Length; i++)
                foreach (var key in Completed[i].Keys)
                    if (!lessons[i].ReviewCandidates.Any(x => x.Key == key))
                        throw new InvalidOperationException($"Missing candidate '{key}' in '{lessons[i].Slug}'. No demo data written.");

            // Capture exactly once; all historical timestamps and deterministic IDs derive from this scenario.
            var seedNow = DateTimeOffset.UtcNow;
            var userId = Id(1);
            db.Users.Add(User.Create(userId, Email, "Alex — Backend Portfolio", new PasswordHasher<object>().HashPassword(new object(), password), seedNow.AddDays(-24)));
            db.LearningProfiles.Add(LearningProfile.Create(Id(2), userId, TargetRole.BackendDeveloper, ExperienceLevel.Junior, 30,
                [(Technology.CSharp, true), (Technology.DotNet, true), (Technology.AspNetCore, true), (Technology.EfCore, true), (Technology.PostgreSql, false)],
                [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], seedNow.AddDays(-24)));
            var cardIndex = 0;
            for (var i = 0; i < lessons.Length; i++)
            {
                var lesson = lessons[i];
                var at = seedNow.AddDays(-Completed[i].DaysAgo);
                var progress = LearningContentProgress.Start(Id(100 + i), userId, lesson.Id, at.AddMinutes(-23));
                progress.Complete(progress.Version, at);
                db.LearningContentProgresses.Add(progress);
                db.LearningContentCompletionEvidence.Add(LearningContentCompletionEvidence.Create(Id(120 + i), userId, lesson.Id, lesson.Title, at));
                if (i < 5)
                {
                    var note = KnowledgeNode.Create(Id(200 + i), userId, null, lesson.Title, i, at.AddMinutes(1));
                    note.UpdateContent(Completed[i].Note, at.AddMinutes(1));
                    db.KnowledgeNodes.Add(note);
                    db.KnowledgeSources.Add(KnowledgeSource.FromLearningContent(note.Id, userId, lesson.Id, Id(220 + i), lesson.Title, at.AddMinutes(1)));
                }
                foreach (var key in Completed[i].Keys)
                {
                    var candidate = lesson.ReviewCandidates.Single(x => x.Key == key);
                    var card = ReviewItem.Create(Id(300 + cardIndex), userId, ReviewResourceType.LearningContent, candidate.Id, at.AddMinutes(2), at.AddMinutes(2));
                    db.ReviewItems.Add(card);
                    db.ReviewLearningContentSources.Add(ReviewLearningContentSource.Create(card.Id, userId, lesson.Id, candidate.Id,
                        key, lesson.Title, candidate.Prompt, candidate.Answer, at.AddMinutes(2)));
                    // First recall is later than learning, never implied by card creation.
                    var first = at.AddDays(2);
                    db.ReviewHistories.Add(ReviewHistory.Create(Id(400 + cardIndex * 2), card.Id, ReviewEvaluation.Good,
                        card.Evaluate(ReviewEvaluation.Good, card.ReviewCount, first), first));
                    var last = seedNow.AddDays(cardIndex < 4 ? -3 : -1);
                    var rating = cardIndex < 4 ? ReviewEvaluation.Hard : ReviewEvaluation.Good;
                    db.ReviewHistories.Add(ReviewHistory.Create(Id(401 + cardIndex * 2), card.Id, rating,
                        card.Evaluate(rating, card.ReviewCount, last), last));
                    cardIndex++;
                }
            }
            db.LearningContentProgresses.Add(LearningContentProgress.Start(Id(150), userId, ongoing.Id, seedNow.AddDays(-2)));

            var plan = StudyPlan.Create(Id(500), userId, "Backend foundations: reliable data access", seedNow.AddDays(-2), null);
            Content[] planned = [ongoing, next, resource];
            for (var i = 0; i < planned.Length; i++)
                plan.EnsureManualItem(Id(510 + i), StudyPlanResourceType.LearningContent, planned[i].Id, 20, plan.Version, seedNow.AddDays(-2));
            db.StudyPlans.Add(plan); // Draft: due recall remains the primary action, not a competing active session.

            var completedAt = seedNow.AddDays(-10);
            var session = StudySession.CreateFromPlan(Id(600), userId, "Tracking decisions and the EF Core reference", 30,
                [new(Id(610), StudyResourceType.LearningContent, lessons[3].Id, lessons[3].Title, 15),
                 new(Id(611), StudyResourceType.LearningContent, historyResource.Id, historyResource.Title, 15)], completedAt.AddMinutes(-25));
            session.Start(completedAt.AddMinutes(-23));
            session.CompleteItem(session.Version, Id(610), Id(620), Id(123), "Explained when the same DbContext needs tracking.", completedAt);
            session.CompleteItem(session.Version, Id(611), Id(621), null, "Read the identity-resolution examples; this marks the study task, not the resource, complete.", completedAt.AddMinutes(11));
            session.UpdateReflection(session.Version, "Use a read-only query by default for lists; check whether the query actually materializes entities before discussing tracking overhead.", completedAt.AddMinutes(11));
            db.StudySessions.Add(session);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            Console.WriteLine($"Created {Email}; anchor {seedNow:O}; 6 completed lessons, 1 in progress, 10 cards (4 due), 20 ratings, 5 notes, 1 Draft plan, 1 completed session.");
            return 0;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
