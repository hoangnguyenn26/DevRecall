using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Development;

public sealed class DemoDataSeeder(DevRecallDbContext dbContext, IPasswordHasher passwordHasher)
{
    public const string Email = "demo@devrecall.local";

    public static void EnsureDevelopmentEnvironment(bool isDevelopment)
    {
        if (!isDevelopment) throw new InvalidOperationException("Demo data can only be seeded in the Development environment.");
    }

    public async Task<bool> SeedAsync(string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 12) throw new ArgumentException("Demo password must contain at least 12 characters.", nameof(password));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == UserEmail.Normalize(Email), cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var userId = Id(1);
        dbContext.Users.Add(User.Create(userId, Email, "Alex Morgan", passwordHasher.Hash(password), now.AddDays(-90)));

        var tags = new[]
        {
            Tag.Create(Id(10), userId, "C#", now.AddDays(-80)),
            Tag.Create(Id(11), userId, "ASP.NET Core", now.AddDays(-79)),
            Tag.Create(Id(12), userId, "Algorithms", now.AddDays(-78))
        };
        dbContext.Tags.AddRange(tags);

        var knowledgeTitles = new[]
        {
            "Backend Engineering", "C# fundamentals", "Dependency injection lifetimes", "Async and cancellation",
            "ASP.NET Core", "Request pipeline", "Problem Details", "Data structures", "Graph traversal",
            "Breadth-first search", "Depth-first search", "Spaced repetition"
        };
        var knowledge = new List<KnowledgeNode>();
        for (var index = 0; index < knowledgeTitles.Length; index++)
        {
            Guid? parentId = index switch { 1 or 4 or 7 or 11 => Id(100), 2 or 3 => Id(101), 5 or 6 => Id(104), 8 => Id(107), 9 or 10 => Id(108), _ => null };
            var node = KnowledgeNode.Create(Id(100 + index), userId, parentId, knowledgeTitles[index], index, now.AddDays(-70 + index));
            node.UpdateContent($"# {knowledgeTitles[index]}\n\nA concise demo note with practical examples and recall prompts.", now.AddDays(-60 + index));
            knowledge.Add(node);
        }
        dbContext.KnowledgeNodes.AddRange(knowledge);
        dbContext.KnowledgeNodeTags.AddRange(
            KnowledgeNodeTag.Create(Id(102), Id(10), now.AddDays(-60)),
            KnowledgeNodeTag.Create(Id(105), Id(11), now.AddDays(-59)),
            KnowledgeNodeTag.Create(Id(109), Id(12), now.AddDays(-58)));

        var interview = new List<InterviewQuestion>();
        var interviewTitles = new[]
        {
            "Explain dependency injection lifetimes", "How does async/await work?", "Describe middleware ordering",
            "What is optimistic concurrency?", "Explain SOLID pragmatically", "How do you design idempotent APIs?",
            "When should you use transactions?", "How do you diagnose a slow query?"
        };
        for (var index = 0; index < interviewTitles.Length; index++)
        {
            var question = InterviewQuestion.Create(Id(200 + index), userId, interviewTitles[index], interviewTitles[index],
                index < 3 ? "C# / ASP.NET Core" : "Backend design", (InterviewQuestionDifficulty)(index % 3 + 1),
                "Connect the answer to a production example.", now.AddDays(-55 + index));
            interview.Add(question);
        }
        dbContext.InterviewQuestions.AddRange(interview);
        for (var index = 0; index < 4; index++)
        {
            var answer = InterviewAnswerVersion.CreateDraft(Id(250 + index), interview[index].Id, 1,
                "Start with the core definition, explain the trade-off, then give a concrete production example.", now.AddDays(-30 + index));
            answer.Publish(now.AddDays(-29 + index));
            dbContext.InterviewAnswerVersions.Add(answer);
            dbContext.InterviewFollowUpQuestions.Add(InterviewFollowUpQuestion.Create(Id(270 + index), interview[index].Id,
                "What failure mode would change your design?", 0, now.AddDays(-28 + index)));
        }

        var dsaTitles = new[] { "Two Sum", "Valid Parentheses", "Number of Islands", "Binary Tree Level Order", "Merge Intervals", "LRU Cache" };
        var dsa = new List<DsaProblem>();
        for (var index = 0; index < dsaTitles.Length; index++)
        {
            var problem = DsaProblem.Create(Id(300 + index), userId, dsaTitles[index],
                $"Practice {dsaTitles[index]} and explain the invariant before coding.", (DsaProblemDifficulty)(index % 3 + 1),
                "Demo catalog", null, index < 2 ? ["Arrays"] : ["Graphs", "Patterns"], now.AddDays(-65 + index));
            dsa.Add(problem);
            dbContext.DsaAttempts.Add(DsaAttempt.Create(Id(350 + index), problem.Id, 1,
                index % 3 == 0 ? DsaAttemptResult.Solved : index % 3 == 1 ? DsaAttemptResult.PartiallySolved : DsaAttemptResult.Failed,
                "C#", "// Deliberately short demo solution", "State the invariant, then iterate.", "O(n)", "O(n)",
                20 + index * 4, "Review edge cases next time.", now.AddDays(-45 + index * 6), now.AddDays(-45 + index * 6)));
        }
        dbContext.DsaProblems.AddRange(dsa);

        var reviewItems = new List<ReviewItem>();
        for (var index = 0; index < 6; index++)
        {
            var resourceType = index < 3 ? ReviewResourceType.KnowledgeNode : index < 5 ? ReviewResourceType.InterviewQuestion : ReviewResourceType.DsaProblem;
            var resourceId = index < 3 ? knowledge[index + 2].Id : index < 5 ? interview[index - 3].Id : dsa[2].Id;
            var item = ReviewItem.Create(Id(400 + index), userId, resourceType, resourceId, now.AddDays(index - 3), now.AddDays(-40));
            if (index >= 3)
            {
                var reviewedAt = now.AddDays(-10 + index);
                var schedule = item.Evaluate(index == 3 ? ReviewEvaluation.Again : ReviewEvaluation.Good, 0, reviewedAt);
                dbContext.ReviewHistories.Add(ReviewHistory.Create(Id(450 + index), item.Id,
                    index == 3 ? ReviewEvaluation.Again : ReviewEvaluation.Good, schedule, reviewedAt));
            }
            reviewItems.Add(item);
        }
        dbContext.ReviewItems.AddRange(reviewItems);

        var contribution = new WeaknessSignalContribution(WeaknessSignalType.DsaFailed, now.AddDays(-3), 30, 1m, 30m);
        var score = new WeaknessScoreBreakdown(68m, 68m, WeaknessLevel.High, 3, now, [contribution]);
        dbContext.WeakTopicProfiles.Add(WeakTopicProfile.Create(Id(500), userId, WeakTopicResourceType.DsaProblem, dsa[2].Id, score, now));
        var reason = RecommendationReason.Create(68m, WeaknessLevel.High, 3, now);
        var recommendation = StudyRecommendation.Create(Id(510), userId, RecommendationResourceType.DsaProblem, dsa[2].Id,
            RecommendationType.RetryDsaProblem, RecommendationPriority.High, reason, now, now.AddDays(14));
        dbContext.StudyRecommendations.Add(recommendation);

        var plan = StudyPlan.CreateFromRecommendations(Id(520), userId, "Strengthen graph traversal",
            [new InitialStudyPlanItem(Id(521), recommendation.Id, StudyPlanResourceType.DsaProblem, dsa[2].Id, 30)], now, now.AddDays(7));
        dbContext.StudyPlans.Add(plan);
        var session = StudySession.CreateFromPlan(Id(530), userId, "Graph traversal practice", 30,
            [new InitialStudySessionItem(Id(531), StudyResourceType.DsaProblem, dsa[2].Id, dsa[2].Title, 30)], now.AddDays(-1));
        session.Start(now.AddHours(-2));
        var completedSession = StudySession.CreateFromPlan(Id(540), userId, "Backend foundations review", 25,
            [new InitialStudySessionItem(Id(541), StudyResourceType.KnowledgeNode, knowledge[2].Id, knowledge[2].Title, 25)], now.AddDays(-6));
        completedSession.Start(now.AddDays(-5).AddMinutes(-30));
        completedSession.CompleteItem(Id(541), "Dependency lifetimes are clear; revisit disposal boundaries.", now.AddDays(-5).AddMinutes(-5));
        completedSession.Complete(now.AddDays(-5));
        completedSession.UpdateReflection(completedSession.Version, "Explaining a concrete production failure made the concepts easier to recall.", now.AddDays(-5));
        dbContext.StudySessions.AddRange(session, completedSession);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
}
