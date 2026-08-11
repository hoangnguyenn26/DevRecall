using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningProfiles;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class LearningProfileConstraintsTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostgreSql_ShouldEnforceOneProfilePerUser()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var userId = Guid.NewGuid();
        context.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
        var profile = Create(userId);
        context.LearningProfiles.Add(profile);
        await context.SaveChangesAsync();

        var duplicateProfile = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_profiles (id, user_id, target_role, experience_level, available_minutes_per_day, version, created_at_utc, updated_at_utc) VALUES ({Guid.NewGuid()}, {userId}, {(int)TargetRole.BackendDeveloper}, {(int)ExperienceLevel.Junior}, 45, 1, {Now}, {Now})");
        await duplicateProfile.Should().ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task PostgreSql_ShouldEnforceUniqueTechnologyPerProfile()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var userId = Guid.NewGuid();
        var profile = Create(userId);
        context.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
        context.LearningProfiles.Add(profile);
        await context.SaveChangesAsync();

        var duplicateTechnology = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_profile_technologies (id, learning_profile_id, technology, is_primary) VALUES ({Guid.NewGuid()}, {profile.Id}, {(int)Technology.CSharp}, false)");
        await duplicateTechnology.Should().ThrowAsync<PostgresException>();
    }

    [Fact]
    public async Task PostgreSql_ShouldEnforceUniqueGoalPerProfile()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var userId = Guid.NewGuid();
        var profile = Create(userId);
        context.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
        context.LearningProfiles.Add(profile);
        await context.SaveChangesAsync();

        var duplicateGoal = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_profile_goals (id, learning_profile_id, goal) VALUES ({Guid.NewGuid()}, {profile.Id}, {(int)LearningProfileGoal.PrepareForInterviews})");
        await duplicateGoal.Should().ThrowAsync<PostgresException>();
    }

    private static LearningProfile Create(Guid userId) => LearningProfile.Create(Guid.NewGuid(), userId,
        TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
        [(Technology.CSharp, true)], [LearningProfileGoal.PrepareForInterviews], Now);
}
