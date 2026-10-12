using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Application.WeakTopics.RecalculateAll;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;
using DevRecall.Domain.Interview.Practice;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Content = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Development;

// Explicit local fixture. Historical outcomes are simulated, never claims about the owner's performance.
public sealed partial class DogfoodDataSeeder(DevRecallDbContext db, IPasswordHasher passwordHasher,
    IWeakTopicCandidateReader weakCandidates, IWeakTopicBatchSignalReader weakSignals,
    IRecommendationCandidateReader recommendationCandidates)
{
    public const string Email = "hoang.playground@devrecall.local";
    public static readonly Guid UserId = Id(1);

    public async Task<bool> SeedAsync(string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 12) throw new ArgumentException("Use at least 12 characters for the local password.", nameof(password));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize concurrent invocations as well as making a sequential replay a no-op.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2026100901)", cancellationToken);
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == UserEmail.Normalize(Email), cancellationToken))
            return false;

        var catalog = await db.LearningContents.AsNoTracking().Include(x => x.ReviewCandidates)
            .Where(x => x.Status == ContentStatus.Published).OrderBy(x => x.Slug).ToListAsync(cancellationToken);
        string[] completedSlugs = ["dependency-injection-fundamentals", "aspnet-core-service-lifetimes", "ef-core-tracking-vs-no-tracking"];
        var lessons = completedSlugs.Select(slug => catalog.SingleOrDefault(x => x.Slug == slug && x.ContentType == LearningContentType.Lesson)
            ?? throw new InvalidOperationException($"Published lesson '{slug}' is required; run --seed-learning-content first.")).ToArray();
        var ongoing = catalog.First(x => x.ContentType == LearningContentType.Lesson && !completedSlugs.Contains(x.Slug));
        var resources = catalog.Where(x => x.ContentType == LearningContentType.ExternalResource).ToArray();
        if (resources.Length < 2) throw new InvalidOperationException("At least two Published external resources are required.");

        var now = DateTimeOffset.UtcNow;
        db.Users.Add(User.Create(UserId, Email, "Hoang Playground", passwordHasher.Hash(password), now.AddDays(-90)));
        db.UserLearningPreferences.Add(UserLearningPreference.Complete(UserId, LearningGoal.PrepareForInterviews, 45, 5,
            [LearningFocusArea.DotNet, LearningFocusArea.AlgorithmsAndDataStructures, LearningFocusArea.InterviewCommunication], now.AddDays(-89)));
        db.LearningProfiles.Add(LearningProfile.Create(Id(2), UserId, TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, true), (Technology.DotNet, true), (Technology.AspNetCore, true), (Technology.EfCore, true), (Technology.PostgreSql, false)],
            [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], now.AddDays(-89)));

        var notes = SeedKnowledge(now);
        var questions = SeedInterview(now);
        var problems = SeedDsa(now);
        SeedReviews(notes, questions, problems, now);
        SeedLearning(lessons, ongoing, resources, now);
        SeedStudy(notes, questions, problems, ongoing, resources, now);
        await db.SaveChangesAsync(cancellationToken);

        // Use the production readers/policies over actual fixture outcomes, not hand-written scores.
        var candidates = await weakCandidates.ReadAsync(UserId, now.AddDays(-90), now, cancellationToken);
        var signals = (await weakSignals.ReadAsync(UserId, now.AddDays(-90), now, cancellationToken))
            .GroupBy(x => (x.ResourceType, x.ResourceId)).ToDictionary(x => x.Key,
                x => x.Select(row => new WeaknessSignal(row.SignalType, row.OccurredAtUtc)).ToArray());
        foreach (var candidate in candidates)
            db.WeakTopicProfiles.Add(WeakTopicProfile.Create(Guid.NewGuid(), UserId, candidate.ResourceType, candidate.ResourceId,
                WeakTopicScoringPolicy.Calculate(signals.GetValueOrDefault((candidate.ResourceType, candidate.ResourceId)) ?? [], now), now));
        await db.SaveChangesAsync(cancellationToken);
        foreach (var candidate in await recommendationCandidates.ReadAsync(UserId, 50, cancellationToken))
            db.StudyRecommendations.Add(StudyRecommendation.Create(Guid.NewGuid(), UserId,
                RecommendationMappingPolicy.MapResourceType(candidate.ResourceType), candidate.ResourceId,
                RecommendationMappingPolicy.MapType(candidate.ResourceType), RecommendationPriorityPolicy.FromWeaknessLevel(candidate.WeaknessLevel),
                RecommendationReason.Create(candidate.WeaknessScore, candidate.WeaknessLevel, candidate.SignalCount, candidate.WeaknessCalculatedAtUtc),
                now, now.Add(RecommendationDefaults.ActiveLifetime)));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private List<KnowledgeNode> SeedKnowledge(DateTimeOffset now)
    {
        var tags = Groups.Select((title, i) => Tag.Create(Id(10 + i), UserId, title, now.AddDays(-85))).ToArray();
        db.Tags.AddRange(tags);
        for (var i = 0; i < Groups.Length; i++)
        {
            var root = KnowledgeNode.Create(Id(100 + i), UserId, null, Groups[i], i, now.AddDays(-80));
            root.UpdateContent($"# {Groups[i]}\n\nA workspace for explanations, trade-offs and interview recall.\n\nThese starting notes are editable sample material; add your own examples as you practice.", now.AddDays(-80));
            db.KnowledgeNodes.Add(root);
        }
        var notes = new List<KnowledgeNode>();
        for (var i = 0; i < Concepts.Length; i++)
        {
            var concept = Concepts[i];
            var at = now.AddDays(-75 + i / 2);
            var note = KnowledgeNode.Create(Id(200 + i), UserId, Id(100 + concept.Group), concept.Title, i % 4, at);
            note.UpdateContent($"# {concept.Title}\n\n## Core idea\n\n{concept.Answer}\n\n## Common mistake\n\n{concept.Pitfall}\n\n## Recall without looking\n\n- {concept.Prompt}\n- Give an example from a backend project.\n- Explain one trade-off before choosing an implementation.\n\n## My next practice\n\nWrite a small example, then explain it aloud in two minutes.", at);
            note.UpdateMetadata(concept.Prompt, null, at);
            notes.Add(note);
            db.KnowledgeNodeTags.Add(KnowledgeNodeTag.Create(note.Id, tags[concept.Group].Id, at));
        }
        db.KnowledgeNodes.AddRange(notes);
        return notes;
    }

    private List<InterviewQuestion> SeedInterview(DateTimeOffset now)
    {
        var questions = new List<InterviewQuestion>();
        for (var i = 0; i < Concepts.Length; i++)
        {
            var concept = Concepts[i];
            var at = now.AddDays(-70 + i / 2);
            var question = InterviewQuestion.Create(Id(400 + i), UserId, concept.Title, concept.Prompt,
                Groups[concept.Group], i % 4 == 0 ? InterviewQuestionDifficulty.Easy : InterviewQuestionDifficulty.Medium,
                concept.Pitfall, at);
            questions.Add(question);
            var published = InterviewAnswerVersion.CreateDraft(Id(500 + i), question.Id, 1, concept.Answer, at.AddHours(1));
            published.Publish(at.AddHours(2));
            db.InterviewAnswerVersions.Add(published);
            db.InterviewAnswerVersions.Add(InterviewAnswerVersion.CreateDraft(Id(600 + i), question.Id, 2,
                $"{concept.Answer}\n\nProduction angle: {concept.Pitfall}\n\nTODO: add a concrete example from my own project.", at.AddDays(1)));
            var followUp = InterviewFollowUpQuestion.Create(Id(700 + i), question.Id, $"How would you demonstrate {concept.Title.ToLowerInvariant()} in a small backend example?", 0, at);
            db.InterviewFollowUpQuestions.Add(followUp);
            db.InterviewFollowUpQuestions.Add(InterviewFollowUpQuestion.Create(Id(800 + i), question.Id, "What failure or edge case would you check before shipping this?", 1, at));
            for (var round = 0; round < 2; round++)
            {
                var completed = now.AddDays(-38 + i + round * 4).AddMinutes(-10);
                var rating = round == 0 ? InterviewSelfRating.Fair : i % 5 == 0 ? InterviewSelfRating.NeedsWork : InterviewSelfRating.Good;
                db.InterviewPracticeAttempts.Add(InterviewPracticeAttempt.Create(Id(900 + i * 2 + round), UserId, question.Id,
                    Id(1100 + i * 2 + round), question.Question, 1,
                    round == 0 ? concept.Answer.Split('.')[0] + ". I need to explain the trade-off more clearly." : concept.Answer,
                    concept.Answer, rating, completed.AddMinutes(-4 - i % 4), completed,
                    [(followUp.Id, followUp.Prompt, concept.Pitfall)], 2));
            }
        }
        db.InterviewQuestions.AddRange(questions);
        return questions;
    }

    private List<DsaProblem> SeedDsa(DateTimeOffset now)
    {
        var problems = new List<DsaProblem>();
        for (var i = 0; i < Problems.Length; i++)
        {
            var definition = Problems[i];
            var problem = DsaProblem.Create(Id(1300 + i), UserId, definition.Title,
                $"{definition.Prompt}\n\nExplain your invariant, cover empty/minimum-size inputs, and state time/space complexity before submitting an attempt.",
                i is 0 or 1 or 3 or 8 or 9 or 14 ? DsaProblemDifficulty.Easy : i is 16 or 18 ? DsaProblemDifficulty.Hard : DsaProblemDifficulty.Medium,
                "Local practice collection", null, [definition.Pattern], now.AddDays(-70));
            problems.Add(problem);
            for (var round = 0; round < 4; round++)
            {
                var at = now.AddDays(-54 + round * 15 + i % 7);
                var result = i % 5 == 0 && round >= 2 ? DsaAttemptResult.Failed
                    : round == 0 ? DsaAttemptResult.Failed : round == 1 ? DsaAttemptResult.PartiallySolved : DsaAttemptResult.Solved;
                db.DsaAttempts.Add(DsaAttempt.Create(Id(1400 + i * 4 + round), problem.Id, round + 1, result, "C#", null,
                    definition.Approach, definition.Time, definition.Space, 42 - round * 6 + i % 5,
                    result == DsaAttemptResult.Solved ? "Simulated baseline: explained the invariant and checked edge cases. Revisit without notes."
                        : "Simulated baseline: missed an edge case; trace the smallest failing input before the next attempt.", at, at));
            }
        }
        db.DsaProblems.AddRange(problems);
        return problems;
    }

    private void SeedReviews(List<KnowledgeNode> notes, List<InterviewQuestion> questions, List<DsaProblem> problems, DateTimeOffset now)
    {
        var sources = notes.Select(x => (ReviewResourceType.KnowledgeNode, x.Id))
            .Concat(questions.Take(8).Select(x => (ReviewResourceType.InterviewQuestion, x.Id)))
            .Concat(problems.Take(4).Select(x => (ReviewResourceType.DsaProblem, x.Id))).ToArray();
        for (var i = 0; i < sources.Length; i++)
        {
            var item = ReviewItem.Create(Id(1800 + i), UserId, sources[i].Item1, sources[i].Id, now.AddDays(-55), now.AddDays(-56));
            for (var round = 0; round < 4; round++)
            {
                var at = now.AddDays(round switch { 0 => -50, 1 => -32, 2 => -15, _ => -2 }).AddHours(-(i % 6));
                var rating = round == 3 && i < 8 ? ReviewEvaluation.Again
                    : round == 0 && i % 3 == 0 ? ReviewEvaluation.Hard : ReviewEvaluation.Good;
                var schedule = item.Evaluate(rating, item.ReviewCount, at);
                db.ReviewHistories.Add(ReviewHistory.Create(Id(1900 + i * 4 + round), item.Id, rating, schedule, at));
            }
            db.ReviewItems.Add(item);
        }
    }

    private void SeedLearning(Content[] lessons, Content ongoing, Content[] resources, DateTimeOffset now)
    {
        for (var i = 0; i < lessons.Length; i++)
        {
            var lesson = lessons[i];
            var completed = now.AddDays(-24 + i * 7);
            var progress = LearningContentProgress.Start(Id(2200 + i), UserId, lesson.Id, completed.AddMinutes(-25));
            progress.Complete(progress.Version, completed);
            var evidence = LearningContentCompletionEvidence.Create(Id(2210 + i), UserId, lesson.Id, lesson.Title, completed);
            db.LearningContentProgresses.Add(progress);
            db.LearningContentCompletionEvidence.Add(evidence);
            var note = KnowledgeNode.Create(Id(2220 + i), UserId, Id(101), $"Lesson takeaway: {lesson.Title}", 4 + i, completed.AddMinutes(1));
            var selected = lesson.ReviewCandidates.OrderBy(x => x.Position).Take(2).ToArray();
            note.UpdateContent(string.Join("\n\n", selected.Select(x => $"## {x.Prompt}\n\n{x.Answer}")), completed.AddMinutes(1));
            db.KnowledgeNodes.Add(note);
            db.KnowledgeSources.Add(KnowledgeSource.FromLearningContent(note.Id, UserId, lesson.Id, Id(2230 + i), lesson.Title, completed.AddMinutes(1)));
            foreach (var candidate in selected)
            {
                var item = ReviewItem.Create(Guid.NewGuid(), UserId, ReviewResourceType.LearningContent, candidate.Id, completed, completed);
                db.ReviewItems.Add(item);
                db.ReviewLearningContentSources.Add(ReviewLearningContentSource.Create(item.Id, UserId, lesson.Id,
                    candidate.Id, candidate.Key, lesson.Title, candidate.Prompt, candidate.Answer, completed));
                var at = now.AddDays(-3);
                var rating = i == 0 ? ReviewEvaluation.Hard : ReviewEvaluation.Easy;
                var schedule = item.Evaluate(rating, 0, at);
                db.ReviewHistories.Add(ReviewHistory.Create(Guid.NewGuid(), item.Id, rating, schedule, at));
            }
            var session = StudySession.CreateFromPlan(Id(2250 + i), UserId, $"Learn and read deeper: {lesson.Title}", 40,
                [new(Id(2260 + i), StudyResourceType.LearningContent, lesson.Id, lesson.Title, 20),
                 new(Id(2270 + i), StudyResourceType.LearningContent, resources[i % resources.Length].Id, resources[i % resources.Length].Title, 20)], completed.AddMinutes(-30));
            session.Start(completed.AddMinutes(-25));
            session.CompleteItem(session.Version, Id(2260 + i), Id(2280 + i), evidence.Id, "Simulated lesson completion.", completed);
            session.CompleteItem(session.Version, Id(2270 + i), Id(2290 + i), null, "Simulated external reading task; no lesson completion evidence.", completed.AddMinutes(12));
            session.UpdateReflection(session.Version, "The internal explanation established the concept; the reference added implementation detail.", completed.AddMinutes(12));
            db.StudySessions.Add(session);
        }
        db.LearningContentProgresses.Add(LearningContentProgress.Start(Id(2300), UserId, ongoing.Id, now.AddHours(-1)));
    }

    private void SeedStudy(List<KnowledgeNode> notes, List<InterviewQuestion> questions, List<DsaProblem> problems,
        Content ongoing, Content[] resources, DateTimeOffset now)
    {
        for (var i = 0; i < 20; i++)
        {
            var at = now.AddDays(-58 + i * 3);
            var session = StudySession.CreateFromPlan(Id(2400 + i), UserId, $"Recall block: {Groups[i % Groups.Length]}", 30,
                [new(Id(2500 + i * 2), StudyResourceType.KnowledgeNode, notes[i].Id, notes[i].Title, 15),
                 new(Id(2501 + i * 2), StudyResourceType.KnowledgeNode, notes[i + 1].Id, notes[i + 1].Title, 15)], at.AddMinutes(-5));
            session.Start(at);
            session.CompleteItem(session.Version, Id(2500 + i * 2), Id(2600 + i * 2), null, "Simulated recall and explanation.", at.AddMinutes(12));
            if (i % 5 == 0)
                session.SkipItem(session.Version, Id(2501 + i * 2), Id(2601 + i * 2), "Simulated baseline: moved this topic to another block.", at.AddMinutes(18));
            else
                session.CompleteItem(session.Version, Id(2501 + i * 2), Id(2601 + i * 2), null, "Simulated example and edge-case discussion.", at.AddMinutes(27 + i % 5));
            session.UpdateReflection(session.Version, "Explain before looking at the note. Next time, connect the concept to a concrete endpoint or query.", session.CompletedAtUtc!.Value);
            db.StudySessions.Add(session);
        }

        string[] planTitles = ["Continue backend foundations", "EF Core reading and practice", "Interview warm-up", "Arrays and sliding windows", "Concurrency and API reliability", "Next focused learning block", "Rescheduled weekend block"];
        for (var i = 0; i < planTitles.Length; i++)
        {
            var at = now.AddDays(-2).AddMinutes(i);
            var plan = StudyPlan.Create(Id(2800 + i), UserId, planTitles[i], at, null);
            plan.EnsureManualItem(Id(2820 + i * 3), StudyPlanResourceType.LearningContent, ongoing.Id, 20, plan.Version, at);
            plan.EnsureManualItem(Id(2821 + i * 3), StudyPlanResourceType.LearningContent, resources[i % resources.Length].Id, 15, plan.Version, at);
            plan.EnsureManualItem(Id(2822 + i * 3), i % 2 == 0 ? StudyPlanResourceType.InterviewQuestion : StudyPlanResourceType.DsaProblem,
                i % 2 == 0 ? questions[i].Id : problems[i].Id, 20, plan.Version, at);
            if (i is 0 or 1 or 2 or 4) plan.MarkReady(plan.Version, at.AddHours(1));
            if (i is 5 or 6) plan.Cancel(plan.Version, at.AddHours(2));
            if (i == 0)
            {
                var session = StudySession.CreateFromPlan(Id(2900), UserId, plan.Title, plan.TotalPlannedDurationMinutes,
                    [new(Id(2901), StudyResourceType.LearningContent, ongoing.Id, ongoing.Title, 20),
                     new(Id(2902), StudyResourceType.LearningContent, resources[0].Id, resources[0].Title, 15),
                     new(Id(2903), StudyResourceType.InterviewQuestion, questions[0].Id, questions[0].Title, 20)], now.AddMinutes(-25));
                plan.MarkConverted(plan.Version, session.Id, now.AddMinutes(-25));
                session.Start(now.AddMinutes(-20));
                session.StartItem(Id(2901), now.AddMinutes(-19));
                db.StudySessions.Add(session);
            }
            db.StudyPlans.Add(plan);
        }
        for (var i = 0; i < 3; i++)
        {
            var session = StudySession.CreateFromPlan(Id(2950 + i), UserId, i == 2 ? "Cancelled practice block" : $"Upcoming practice: {Problems[i].Pattern}", 35,
                [new(Id(2960 + i), StudyResourceType.DsaProblem, problems[i].Id, problems[i].Title, 35)], now.AddDays(-1));
            if (i == 2) session.Cancel(now.AddHours(-3));
            db.StudySessions.Add(session);
        }
    }

    private static Guid Id(int value) => Guid.Parse($"d06f0000-0000-0000-0000-{value:D12}");
}
