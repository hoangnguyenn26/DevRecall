using DevRecall.Application.Identity;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningProfiles;
using FluentAssertions;

namespace DevRecall.Application.Tests.LearningProfiles;

public sealed class LearningProfileSignalsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShouldReturnEmptySignalsForMissingProfile()
    {
        var userId = Guid.NewGuid();
        var repository = new RepositoryStub(null);
        var handler = new GetCurrentLearningProfileSignalsHandler(repository, new CurrentUserStub(userId));

        var result = await handler.HandleAsync(CancellationToken.None);

        result.IsConfigured.Should().BeFalse();
        result.TargetRole.Should().BeNull();
        result.PrimaryTechnologies.Should().BeEmpty();
        result.OtherTechnologies.Should().BeEmpty();
        repository.RequestedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_ShouldPreserveCanonicalDeclaredSignalsAndSeparateTechnologyPriority()
    {
        var userId = Guid.NewGuid();
        var profile = LearningProfile.Create(Guid.NewGuid(), userId,
            TargetRole.BackendDeveloper, ExperienceLevel.Junior, 45,
            [(Technology.CSharp, true), (Technology.DotNet, true), (Technology.PostgreSql, false)],
            [LearningProfileGoal.PrepareForInterviews, LearningProfileGoal.ImproveBackendFundamentals], Now);
        var handler = new GetCurrentLearningProfileSignalsHandler(
            new RepositoryStub(profile), new CurrentUserStub(userId));

        var result = await handler.HandleAsync(CancellationToken.None);

        result.IsConfigured.Should().BeTrue();
        result.TargetRole.Should().Be(TargetRole.BackendDeveloper);
        result.ExperienceLevel.Should().Be(ExperienceLevel.Junior);
        result.PrimaryTechnologies.Should().BeEquivalentTo([Technology.CSharp, Technology.DotNet]);
        result.OtherTechnologies.Should().BeEquivalentTo([Technology.PostgreSql]);
        result.Goals.Should().BeEquivalentTo([LearningProfileGoal.PrepareForInterviews,
            LearningProfileGoal.ImproveBackendFundamentals]);
        result.AvailableMinutesPerDay.Should().Be(45);
    }

    private sealed class RepositoryStub(LearningProfile? profile) : ILearningProfileRepository
    {
        public Guid? RequestedUserId { get; private set; }
        public Task<LearningProfile?> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            return Task.FromResult(profile);
        }
        public void Add(LearningProfile value) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = userId;
    }
}
