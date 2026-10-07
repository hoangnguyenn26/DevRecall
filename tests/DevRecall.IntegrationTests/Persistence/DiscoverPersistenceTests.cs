using DevRecall.Application.Discover;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure;
using DevRecall.Infrastructure.Development;
using DevRecall.Infrastructure.Persistence;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit.Abstractions;
using Content = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class DiscoverPersistenceTests(PostgreSqlFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task Reader_ShouldExcludeUnpublishedExternalAndUserProgressWithoutTrackingOrWrites()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Database"] = fixture.ConnectionString }).Build();
        await using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await new LearningContentSeeder(db).SeedAsync();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        db.Users.AddRange(User.Create(userId, $"{userId}@example.com", "Learner", "hash", now),
            User.Create(otherId, $"{otherId}@example.com", "Other", "hash", now));
        db.LearningProfiles.Add(LearningProfile.Create(Guid.NewGuid(), userId, TargetRole.BackendDeveloper,
            ExperienceLevel.Beginner, 15, [(Technology.Vue, true)],
            [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], now));
        var topicId = await db.ContentTopics.Select(item => item.Id).FirstAsync();
        var drafts = Enumerable.Range(0, 4).Select(index => Content.CreateDraft(Guid.NewGuid(),
            $"discover-test-{Guid.NewGuid():N}", "Discover test lesson", "A test of discovery eligibility.",
            index == 3 ? LearningContentType.ExternalResource : LearningContentType.Lesson,
            ContentDifficulty.Advanced, 90, index == 3 ? ContentSourceType.External : ContentSourceType.Internal,
            "Test", index == 3 ? "https://example.com" : null, [Technology.DotNet], [topicId],
            index == 3 ? [] : [new("Understand eligibility.")],
            index == 3 ? [] : [new(LearningContentSectionType.Explanation, null, "Lesson body")], now,
            resourceKind: index == 3 ? ExternalResourceKind.Documentation : null)).ToArray();
        foreach (var item in drafts) item.SetGoals([LearningProfileGoal.ImproveBackendFundamentals], now);
        drafts[1].Publish(now);
        drafts[1].Archive(now);
        drafts[2].Publish(now); // Advanced, 90-minute content remains eligible for a Beginner / 15-min profile.
        drafts[3].Publish(now);
        db.LearningContents.AddRange(drafts);
        var seeded = await db.LearningContents.Where(item => item.ContentType == LearningContentType.Lesson)
            .OrderByDescending(item => item.PublishedAtUtc).Take(3).ToArrayAsync();
        db.LearningContentProgresses.AddRange(
            LearningContentProgress.Start(Guid.NewGuid(), userId, seeded[0].Id, now),
            LearningContentProgress.CompleteDirectly(Guid.NewGuid(), userId, seeded[1].Id, now),
            LearningContentProgress.Start(Guid.NewGuid(), otherId, drafts[2].Id, now));
        db.LearningContentCompletionEvidence.Add(LearningContentCompletionEvidence.Create(Guid.NewGuid(),
            userId, seeded[2].Id, seeded[2].Title, now)); // Evidence-only inconsistency is also excluded.
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var reader = scope.ServiceProvider.GetRequiredService<IDiscoverReader>();
        var inputs = await reader.GetAsync(userId, CancellationToken.None);
        var result = LearningRecommendationPolicy.Build(inputs);
        var items = result.Recommended.Concat(result.BasedOnGoals).ToArray();
        result.ProfileConfigured.Should().BeTrue();
        items.Should().HaveCount(7); // Four recommended and three goal choices from the expanded internal catalog.
        result.Recommended.Should().HaveCount(4);
        items.Should().Contain(item => item.Slug == drafts[2].Slug && item.EstimatedMinutes == 90);
        items.Should().NotContain(item => seeded.Select(value => value.Slug).Contains(item.Slug));
        items.Should().NotContain(item => item.Slug == drafts[0].Slug || item.Slug == drafts[1].Slug || item.Slug == drafts[3].Slug);
        items.Select(item => item.Slug).Should().OnlyHaveUniqueItems();
        db.ChangeTracker.Entries().Should().BeEmpty();
        (await db.LearningContentProgresses.CountAsync()).Should().Be(3);
        (await db.LearningContentCompletionEvidence.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GoalTags_ShouldRejectDuplicateAndInvalidCanonicalValues()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await new LearningContentSeeder(db).SeedAsync();
        var contentId = await db.LearningContents.Select(item => item.Id).FirstAsync();
        var duplicate = () => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_content_goals (learning_content_id, goal) VALUES ({contentId}, {2})");
        var exception = await duplicate.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        await transaction.RollbackAsync();
        db.ChangeTracker.Clear();
        await using var nextTransaction = await db.Database.BeginTransactionAsync();
        await new LearningContentSeeder(db).SeedAsync();
        var invalid = () => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_content_goals (learning_content_id, goal) VALUES ({contentId}, {99})");
        (await invalid.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task CandidatePool_ShouldBeBoundedBeforeMaterializationAndSeedUpgradeShouldBeIdempotent()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Database"] = fixture.ConnectionString }).Build();
        await using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var seeder = new LearningContentSeeder(db);
        await seeder.SeedAsync();
        var legacy = await db.LearningContents.Include(item => item.Goals)
            .SingleAsync(item => item.Slug == "ef-core-transactions");
        legacy.SetGoals([LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();
        await seeder.SeedAsync();
        legacy.Goals.Should().Contain(item => item.Goal == LearningProfileGoal.BuildProjects);
        var version = legacy.Version;
        await seeder.SeedAsync();
        legacy.Version.Should().Be(version);
        // Inspect the real seeded catalog without changing an actual account's preferences.
        var catalog = await db.LearningContents.AsNoTracking().Include(item => item.Goals)
            .Include(item => item.Technologies).Include(item => item.Topics)
            .AsSplitQuery().ToArrayAsync();
        var topics = await db.ContentTopics.AsNoTracking().ToDictionaryAsync(item => item.Id);
        var candidates = catalog.Select(item => new DiscoverCandidate(item.Id, item.Slug, item.Title, item.Summary,
            item.Difficulty, item.EstimatedMinutes, item.PublishedAtUtc!.Value,
            item.Technologies.Select(tag => tag.Technology).ToArray(), item.Goals.Select(tag => tag.Goal).ToArray(),
            item.Topics.Select(tag => new DiscoverTopic(tag.TopicId, topics[tag.TopicId].Slug, topics[tag.TopicId].Name)).ToArray())).ToArray();
        var personas = new (string Name, Technology[] Primary, LearningProfileGoal[] Goals, ExperienceLevel? Level, int? Minutes)[]
        {
            ("Backend Junior", [Technology.CSharp, Technology.DotNet, Technology.AspNetCore], [LearningProfileGoal.ImproveBackendFundamentals], ExperienceLevel.Junior, 30),
            ("EF broad focus", [Technology.CSharp, Technology.DotNet, Technology.EfCore, Technology.PostgreSql], [LearningProfileGoal.ImproveBackendFundamentals], ExperienceLevel.Junior, 30),
            ("EF specific focus", [Technology.EfCore], [LearningProfileGoal.ImproveBackendFundamentals], ExperienceLevel.MidLevel, 30),
            ("Interview", [Technology.CSharp, Technology.DotNet], [LearningProfileGoal.PrepareForInterviews], ExperienceLevel.Junior, 30),
            ("Sparse .NET", [Technology.DotNet], [], null, null),
            ("No signals", [], [], null, null)
        };
        foreach (var persona in personas)
        {
            var signals = new LearningProfileSignals(null, persona.Level, persona.Primary.ToHashSet(),
                new HashSet<Technology>(), persona.Goals.ToHashSet(), persona.Minutes, persona.Level is not null);
            var recommendation = LearningRecommendationPolicy.Build(new(signals, [], candidates));
            output.WriteLine($"{persona.Name}: {string.Join(" | ", recommendation.Recommended.Select(item => item.Slug))}");
        }
        var now = DateTimeOffset.UtcNow.AddDays(1);
        var userId = Guid.NewGuid();
        db.Users.Add(User.Create(userId, $"{userId}@example.com", "Learner", "hash", now));
        db.LearningProfiles.Add(LearningProfile.Create(Guid.NewGuid(), userId, TargetRole.BackendDeveloper,
            ExperienceLevel.Junior, 30, [(Technology.DotNet, true)], [LearningProfileGoal.ImproveBackendFundamentals], now));
        var topicId = await db.ContentTopics.Select(item => item.Id).FirstAsync();
        for (var index = 0; index < 101; index++)
        {
            var content = Content.CreateDraft(Guid.NewGuid(), $"bounded-{Guid.NewGuid():N}", "Bounded candidate lesson",
                "Candidate metadata only", LearningContentType.Lesson, ContentDifficulty.Beginner, 10,
                ContentSourceType.Internal, "Test", null, [Technology.DotNet], [topicId],
                [new("Test bounding")], [new(LearningContentSectionType.Explanation, null, "Not projected")], now);
            content.Publish(now);
            db.LearningContents.Add(content);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var inputs = await scope.ServiceProvider.GetRequiredService<IDiscoverReader>().GetAsync(userId, CancellationToken.None);
        inputs.Candidates.Should().HaveCount(100);
        inputs.Candidates.Should().OnlyContain(item => item.Slug.StartsWith("bounded-", StringComparison.Ordinal));
        var result = LearningRecommendationPolicy.Build(inputs);
        result.Recommended.Should().HaveCount(4);
        result.Recommended.Should().OnlyContain(item => item.Reasons.Count >= 1 && item.Reasons.Count <= 2);
        db.ChangeTracker.Entries().Should().BeEmpty();
    }
}
