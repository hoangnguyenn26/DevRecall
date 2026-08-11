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
        var exception = await duplicateProfile.Should().ThrowAsync<PostgresException>();
        exception.Which.ConstraintName.Should().Be("uq_learning_profiles_user_id");
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
        var exception = await duplicateTechnology.Should().ThrowAsync<PostgresException>();
        exception.Which.ConstraintName.Should().Be(
            "uq_learning_profile_technologies_profile_technology");
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
        var exception = await duplicateGoal.Should().ThrowAsync<PostgresException>();
        exception.Which.ConstraintName.Should().Be("uq_learning_profile_goals_profile_goal");
    }

    [Fact]
    public async Task PostgreSql_ShouldRejectNonCanonicalStudyTime()
    {
        await using var context = fixture.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var userId = Guid.NewGuid();
        context.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
        await context.SaveChangesAsync();

        var insert = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO learning_profiles (id, user_id, target_role, experience_level, available_minutes_per_day, version, created_at_utc, updated_at_utc) VALUES ({Guid.NewGuid()}, {userId}, {(int)TargetRole.BackendDeveloper}, {(int)ExperienceLevel.Junior}, 47, 1, {Now}, {Now})");

        var exception = await insert.Should().ThrowAsync<PostgresException>();
        exception.Which.ConstraintName.Should().Be("ck_learning_profiles_minutes");
    }

    [Fact]
    public async Task PostgreSql_ShouldEnforceProfileOptimisticConcurrency()
    {
        var userId = Guid.NewGuid();
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
            setup.LearningProfiles.Add(Create(userId));
            await setup.SaveChangesAsync();
        }

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = await firstContext.LearningProfiles.Include(item => item.Technologies)
            .Include(item => item.Goals).SingleAsync(item => item.UserId == userId);
        var second = await secondContext.LearningProfiles.Include(item => item.Technologies)
            .Include(item => item.Goals).SingleAsync(item => item.UserId == userId);
        first.Update(TargetRole.BackendDeveloper, ExperienceLevel.Junior, 60,
            [(Technology.CSharp, true)], [LearningProfileGoal.PrepareForInterviews], Now.AddMinutes(1));
        second.Update(TargetRole.BackendDeveloper, ExperienceLevel.Junior, 90,
            [(Technology.CSharp, true)], [LearningProfileGoal.PrepareForInterviews], Now.AddMinutes(2));
        await firstContext.SaveChangesAsync();

        var staleSave = () => secondContext.SaveChangesAsync();
        await staleSave.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task PostgreSql_ShouldAllowOnlyOneWinnerInCreateRace()
    {
        var userId = Guid.NewGuid();
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(User.Create(userId, $"profile-{userId:N}@example.com", "Learner", "hash", Now));
            await setup.SaveChangesAsync();
        }

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        firstContext.LearningProfiles.Add(Create(userId));
        secondContext.LearningProfiles.Add(Create(userId));
        await firstContext.SaveChangesAsync();

        var secondCreate = () => secondContext.SaveChangesAsync();
        await secondCreate.Should().ThrowAsync<DbUpdateException>();
    }

    private static LearningProfile Create(Guid userId) => LearningProfile.Create(Guid.NewGuid(), userId,
        TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
        [(Technology.CSharp, true)], [LearningProfileGoal.PrepareForInterviews], Now);
}
