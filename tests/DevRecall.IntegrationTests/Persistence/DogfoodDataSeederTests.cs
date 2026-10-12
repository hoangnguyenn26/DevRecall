using DevRecall.Domain.LearningContent;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure;
using DevRecall.Infrastructure.Development;
using DevRecall.Infrastructure.Persistence;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class DogfoodDataSeederTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Playground_ShouldPreserveReplayAndSeparateExternalTasksFromLessonEvidence()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:Database"] = fixture.ConnectionString }).Build();
        await using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DevRecallDbContext>();
        await scope.ServiceProvider.GetRequiredService<LearningContentSeeder>().SeedAsync();
        var contentCount = await db.LearningContents.CountAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<DogfoodDataSeeder>();
        (await seeder.SeedAsync("test-only-playground-password")).Should().BeTrue();
        var firstUser = await db.Users.AsNoTracking().SingleAsync(x => x.Id == DogfoodDataSeeder.UserId);
        var historyCount = await db.ReviewHistories.CountAsync();
        db.ChangeTracker.Clear();
        (await seeder.SeedAsync("a-different-password-must-not-reset-it")).Should().BeFalse();
        (await db.Users.AsNoTracking().SingleAsync(x => x.Id == DogfoodDataSeeder.UserId)).PasswordHash.Should().Be(firstUser.PasswordHash);
        (await db.ReviewHistories.CountAsync()).Should().Be(historyCount);
        (await db.LearningContents.CountAsync()).Should().Be(contentCount);
        (await db.KnowledgeNodes.CountAsync(x => x.UserId == DogfoodDataSeeder.UserId)).Should().Be(43);
        (await db.DsaAttempts.CountAsync(x => db.DsaProblems.Any(p => p.Id == x.DsaProblemId && p.UserId == DogfoodDataSeeder.UserId))).Should().Be(80);
        (await db.LearningContentCompletionEvidence.CountAsync(x => x.UserId == DogfoodDataSeeder.UserId)).Should().Be(3);
        (await db.LearningContentProgresses.CountAsync(x => x.UserId == DogfoodDataSeeder.UserId && x.Status == LearningProgressStatus.InProgress)).Should().Be(1);
        var externalIds = await db.LearningContents.Where(x => x.ContentType == LearningContentType.ExternalResource).Select(x => x.Id).ToArrayAsync();
        (await db.LearningContentProgresses.AnyAsync(x => x.UserId == DogfoodDataSeeder.UserId && externalIds.Contains(x.LearningContentId))).Should().BeFalse();
        var externalTasks = await db.StudySessionItems.Where(x => x.ResourceType == StudyResourceType.LearningContent
            && externalIds.Contains(x.ResourceId) && x.Status == StudySessionItemStatus.Completed
            && db.StudySessions.Any(s => s.Id == x.StudySessionId && s.UserId == DogfoodDataSeeder.UserId)).ToListAsync();
        externalTasks.Should().HaveCount(3).And.OnlyContain(x => x.EvidenceId == null);
        (await db.StudyRecommendations.AnyAsync(x => x.UserId == DogfoodDataSeeder.UserId)).Should().BeTrue();
    }
}
