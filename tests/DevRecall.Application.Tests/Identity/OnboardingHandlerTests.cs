using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Identity.Onboarding;
using DevRecall.Domain.Identity;
using FluentAssertions;

namespace DevRecall.Application.Tests.Identity;

public sealed class OnboardingHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Complete_WithSameNormalizedPayload_ShouldNotSaveAgain()
    {
        var preference = UserLearningPreference.Complete(
            UserId, LearningGoal.PracticeAlgorithms, 45, 5,
            [LearningFocusArea.Databases, LearningFocusArea.DotNet], Now);
        var repository = new RepositoryStub(preference);
        var handler = new OnboardingHandler(
            repository, new CurrentUserStub(), new ClockStub());

        await handler.CompleteAsync(new(
            "PracticeAlgorithms", 45, 5, ["DotNet", "Databases", "DotNet"]),
            CancellationToken.None);

        repository.SaveCount.Should().Be(0);
        preference.Version.Should().Be(1);
        preference.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Complete_WithChangedPayload_ShouldSaveOnce()
    {
        var preference = UserLearningPreference.Complete(
            UserId, LearningGoal.PracticeAlgorithms, 45, 5,
            [LearningFocusArea.DotNet], Now);
        var repository = new RepositoryStub(preference);
        var handler = new OnboardingHandler(
            repository, new CurrentUserStub(), new ClockStub());

        await handler.CompleteAsync(new(
            "PracticeAlgorithms", 60, 6, ["DotNet"]), CancellationToken.None);

        repository.SaveCount.Should().Be(1);
        preference.Version.Should().Be(2);
    }

    private sealed class RepositoryStub(UserLearningPreference preference)
        : IUserLearningPreferenceRepository
    {
        public int SaveCount { get; private set; }
        public Task<UserLearningPreference?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<UserLearningPreference?>(preference);
        public void Add(UserLearningPreference item) => throw new InvalidOperationException();
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => OnboardingHandlerTests.UserId;
    }

    private sealed class ClockStub : IUtcClock
    {
        public DateTimeOffset UtcNow => Now.AddMinutes(5);
    }
}
