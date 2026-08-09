using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DevRecall.IntegrationTests.Persistence;

public sealed class DatabaseIntegrityModelTests
{
    private readonly IModel _model;

    public DatabaseIntegrityModelTests()
    {
        var options = new DbContextOptionsBuilder<DevRecallDbContext>()
            .UseNpgsql("Host=localhost;Database=dummy;Username=dummy;Password=dummy")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new DevRecallDbContext(options);
        _model = context.Model;
    }

    [Fact]
    public void IdentityAndKnowledgeNaturalKeysShouldBeDatabaseEnforced()
    {
        Index<User>(nameof(User.NormalizedEmail)).Should().Match<IIndex>(index =>
            index.IsUnique && index.GetDatabaseName() == "ux_users_normalized_email");
        Index<Tag>(nameof(Tag.UserId), nameof(Tag.NormalizedName)).Should().Match<IIndex>(index =>
            index.IsUnique && index.GetDatabaseName() == "ux_tags_user_id_normalized_name");
        Entity<UserLearningPreference>().FindPrimaryKey()!.Properties
            .Select(property => property.Name).Should().Equal(nameof(UserLearningPreference.UserId));
    }

    [Fact]
    public void HotPathIndexesShouldMatchOwnerScopedQueryShapes()
    {
        Index<ReviewItem>(nameof(ReviewItem.UserId), nameof(ReviewItem.Status), nameof(ReviewItem.DueAtUtc))
            .GetDatabaseName().Should().Be("ix_review_items_user_status_due_at");
        Index<StudySession>(nameof(StudySession.UserId), nameof(StudySession.CompletedAtUtc))
            .GetDatabaseName().Should().Be("ix_study_sessions_user_completed_at");
        Index<WeakTopicProfile>(nameof(WeakTopicProfile.UserId), nameof(WeakTopicProfile.ResourceType), nameof(WeakTopicProfile.ResourceId))
            .IsUnique.Should().BeTrue();
        Index<StudyRecommendation>(nameof(StudyRecommendation.UserId), nameof(StudyRecommendation.Status), nameof(StudyRecommendation.Priority), nameof(StudyRecommendation.PriorityScore))
            .GetDatabaseName().Should().Be("ix_study_recommendations_user_status_priority_score");
    }

    [Fact]
    public void EditableAggregatesShouldUseOptimisticConcurrencyTokens()
    {
        Property<KnowledgeNode>(nameof(KnowledgeNode.Version)).IsConcurrencyToken.Should().BeTrue();
        Property<ReviewItem>(nameof(ReviewItem.ReviewCount)).IsConcurrencyToken.Should().BeTrue();
        Property<StudySession>(nameof(StudySession.Version)).IsConcurrencyToken.Should().BeTrue();
        Property<StudyRecommendation>(nameof(StudyRecommendation.Version)).IsConcurrencyToken.Should().BeTrue();
        Property<WeakTopicProfile>(nameof(WeakTopicProfile.Version)).IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void EveryDateTimeOffsetShouldMapToTimestampWithTimeZone()
    {
        var dateProperties = _model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => Nullable.GetUnderlyingType(property.ClrType) == typeof(DateTimeOffset)
                || property.ClrType == typeof(DateTimeOffset));

        dateProperties.Should().NotBeEmpty().And.OnlyContain(property =>
            property.GetColumnType() == "timestamp with time zone");
    }

    private IEntityType Entity<TEntity>() =>
        _model.FindEntityType(typeof(TEntity))!;

    private IProperty Property<TEntity>(string name) =>
        Entity<TEntity>().FindProperty(name)!;

    private IIndex Index<TEntity>(params string[] properties) =>
        Entity<TEntity>().GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(properties));
}
