using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Development;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class LearningContentPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public void Seeder_ShouldRejectNonDevelopmentEnvironment()
    {
        var action = () => LearningContentSeeder.EnsureDevelopmentEnvironment(false);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConsistencyRepair_ShouldRejectNonDevelopmentEnvironment()
    {
        var action = () => LearningContentConsistencyRepair.EnsureDevelopmentEnvironment(false);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Seeder_ShouldCreateFivePublishedLessonsIdempotently()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var seeder = new LearningContentSeeder(context);

        var first = await seeder.SeedAsync();
        var second = await seeder.SeedAsync();

        first.Should().Be(5);
        second.Should().Be(0);
        (await context.LearningContents.CountAsync()).Should().Be(5);
        (await context.ContentTopics.CountAsync()).Should().Be(4);
        (await context.LearningContentSections.CountAsync()).Should().BeGreaterThanOrEqualTo(20);
        (await context.LearningContentReviewCandidates.CountAsync()).Should().BeGreaterThanOrEqualTo(14);
    }

    [Fact]
    public async Task PostgreSql_ShouldEnforceSlugAndTechnologyUniqueness()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await new LearningContentSeeder(context).SeedAsync();
        var contentId = await context.LearningContents.Select(item => item.Id).FirstAsync();
        var slug = await context.LearningContents.Select(item => item.Slug).FirstAsync();

        var duplicateSlug = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_contents (id, slug, title, summary, content_type, difficulty, estimated_minutes, status, source_type, source_name, version, created_at_utc, updated_at_utc) VALUES ({Guid.NewGuid()}, {slug}, {'A' + " valid title"}, {'A' + " valid summary"}, 1, 1, 10, 1, 1, {'D' + "evRecall"}, 1, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})");
        var slugException = await duplicateSlug.Should().ThrowAsync<PostgresException>();
        slugException.Which.ConstraintName.Should().Be("uq_learning_contents_slug");

        await transaction.RollbackAsync();
        await using var secondContext = fixture.CreateDbContext();
        await using var secondTransaction = await secondContext.Database.BeginTransactionAsync();
        await new LearningContentSeeder(secondContext).SeedAsync();
        contentId = await secondContext.LearningContents.Select(item => item.Id).FirstAsync();
        var technology = await secondContext.LearningContentTechnologies
            .Where(item => item.LearningContentId == contentId).Select(item => (int)item.Technology).FirstAsync();
        var duplicateTechnology = () => secondContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_content_technologies (id, learning_content_id, technology) VALUES ({Guid.NewGuid()}, {contentId}, {technology})");
        var technologyException = await duplicateTechnology.Should().ThrowAsync<PostgresException>();
        technologyException.Which.ConstraintName.Should().Be("uq_learning_content_technologies_content_technology");
    }

    [Fact]
    public async Task ExplicitRepair_ShouldRestoreBothConsistencyDirectionsIdempotently()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await new LearningContentSeeder(context).SeedAsync();
        var now = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        context.Users.Add(User.Create(userId, $"repair-{userId:N}@example.com", "Repair User", "hash", now));
        var content = await context.LearningContents.OrderBy(item => item.Id).Take(3).ToArrayAsync();
        context.LearningContentProgresses.Add(
            LearningContentProgress.CompleteDirectly(Guid.NewGuid(), userId, content[0].Id, now));
        context.LearningContentProgresses.Add(
            LearningContentProgress.Start(Guid.NewGuid(), userId, content[1].Id, now.AddMinutes(-5)));
        context.LearningContentCompletionEvidence.AddRange(
            LearningContentCompletionEvidence.Create(Guid.NewGuid(), userId, content[1].Id,
                content[1].Title, now),
            LearningContentCompletionEvidence.Create(Guid.NewGuid(), userId, content[2].Id,
                content[2].Title, now.AddMinutes(1)));
        await context.SaveChangesAsync();

        var first = await new LearningContentConsistencyRepair(context).RepairAsync();
        var second = await new LearningContentConsistencyRepair(context).RepairAsync();

        first.Should().Be(new LearningContentConsistencyReport(1, 2, 2, 1));
        second.Should().Be(new LearningContentConsistencyReport(0, 0, 0, 0));
        (await context.LearningContentProgresses.CountAsync(item => item.UserId == userId
            && item.Status == LearningProgressStatus.Completed)).Should().Be(3);
        (await context.LearningContentCompletionEvidence.CountAsync(item => item.UserId == userId))
            .Should().Be(3);
    }
}
